open System
open Microsoft.AspNetCore.Http
open Saturn
open System.Xml
open System.ServiceModel.Syndication
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Npgsql.FSharp

open Shared

type User =
    { Id: string
      Username: string
      Password: string }

type RssUrl =
    { Id: string
      Url: string
      UserId: string }

let connectionString = Environment.GetEnvironmentVariable "DB_CONNECTION_STRING"

let parseRSS (url: string) =
    async {
        use reader = XmlReader.Create url

        return
            (SyndicationFeed.Load reader).Items
            |> Seq.map (fun item ->
                { Title = item.Title.Text
                  LastUpdatedTime = item.LastUpdatedTime.DateTime
                  Link =
                    match item.Links |> Seq.tryHead with
                    | Some first -> first.Uri.AbsoluteUri
                    | None -> "-" })
    }

module DataAccess =

    let getUser (connectionString: string) (loginForm: LoginForm) =
        connectionString
        |> Sql.connect
        |> Sql.query "SELECT id, username, password FROM users WHERE username = @username"
        |> Sql.parameters [ "@username", Sql.string loginForm.Username ]
        |> Sql.execute (fun read ->
            { Id = read.string "id"
              Username = read.text "username"
              Password = read.text "password" })
        |> List.tryHead

    let insertUser (connectionString: string) (loginForm: LoginForm) =
        let newUid = Guid.NewGuid().ToString()

        connectionString
        |> Sql.connect
        |> Sql.query "INSERT INTO users (id, username, password) VALUES (@id, @username, @password)"
        |> Sql.parameters
            [ "@id", Sql.text newUid
              "@username", Sql.text loginForm.Username
              "@password", Sql.text loginForm.Password ]
        |> Sql.executeNonQuery
        |> ignore

        newUid

    let getRSSUrls (connectionString: string) (userId: string) =
        connectionString
        |> Sql.connect
        |> Sql.query "SELECT url FROM rss_urls WHERE user_id = @user_id"
        |> Sql.parameters [ "@user_id", Sql.string userId ]
        |> Sql.execute (fun read -> read.text "url")

    let insertUrls (connectionString: string) (userId: string) (urls: string array) =
        connectionString
        |> Sql.connect
        |> Sql.executeTransaction
            [ "INSERT INTO rss_urls (id, url, user_id) VALUES (@id, @url, @user_id)",
              [ yield!
                    urls
                    |> Array.map (fun url ->
                        [ "@id", Sql.text (Guid.NewGuid().ToString())
                          "@url", Sql.text url
                          "@user_id", Sql.text userId ]) ] ]
        |> ignore

    let deleteUrls (connectionString: string) (userId: string) (urls: string array) =
        connectionString
        |> Sql.connect
        |> Sql.query "DELETE FROM rss_urls WHERE user_id = @user_id AND url = ANY(@urls)"
        |> Sql.parameters [ "@user_id", Sql.text userId; "@urls", Sql.stringArray urls ]
        |> Sql.executeNonQuery
        |> ignore

    let insertSession (connectionString: string) (userId: string) (sessionId: string) =
        connectionString
        |> Sql.connect
        |> Sql.query "INSERT INTO sessions (id, user_id) VALUES (@id, @user_id)"
        |> Sql.parameters [ "@id", Sql.text sessionId; "@user_id", Sql.text userId ]
        |> Sql.executeNonQuery
        |> ignore

    let getUserSession (connectionString: string) (sessionId) =
        connectionString
        |> Sql.connect
        |> Sql.query
            "SELECT u.id AS user_id, u.username AS user_username, u.password AS user_password FROM users u LEFT JOIN sessions s ON s.user_id = u.id WHERE s.id = @session_id"
        |> Sql.parameters [ "@session_id", Sql.string sessionId ]
        |> Sql.execute (fun read ->
            { Id = read.string "user_id"
              Username = read.text "user_username"
              Password = read.text "user_password" })
        |> List.tryHead

module Handler =
    let getRSSList (urls: string array) =
        async {
            let! rssList = urls |> Seq.map (fun url -> parseRSS url |> Async.Catch) |> Async.Parallel

            return
                rssList
                |> Seq.map (function
                    | Choice1Of2 rss -> rss
                    | Choice2Of2 _ -> Seq.empty)
                |> Seq.fold (fun acc elem -> Seq.concat [ acc; elem ]) []
                |> Seq.sortByDescending (fun rss -> rss.LastUpdatedTime)
        }

    let register (connectionString: string) (sessionId: string) (loginForm: LoginForm) : LoginResponse =
        let userId = (DataAccess.insertUser connectionString loginForm)

        let loginResult =
            { LoginResult.UserId = userId
              LoginResult.RssUrls = Array.empty
              LoginResult.SessionId = sessionId }

        Success loginResult

    let login (connectionString: string) (sessionId: string) (loginForm: LoginForm) (user: User) : LoginResponse =
        if user.Password = loginForm.Password then
            let loginResult =
                { LoginResult.UserId = user.Id
                  LoginResult.RssUrls = (DataAccess.getRSSUrls connectionString user.Id) |> List.toArray
                  LoginResult.SessionId = sessionId }

            Success loginResult
        else
            let loginError = { LoginError.Message = "Password not match." }
            Failed loginError

    let loginOrRegister (connectionString: string) (loginForm: LoginForm) : LoginResponse Async =
        async {
            let sessionId = Guid.NewGuid().ToString()

            let loginResponse =
                match DataAccess.getUser connectionString loginForm with
                | None -> register connectionString sessionId loginForm
                | Some user -> login connectionString sessionId loginForm user

            match loginResponse with
            | Success(user: LoginResult) -> DataAccess.insertSession connectionString user.UserId sessionId
            | _ -> ()

            return loginResponse
        }

    let saveRSSUrls (connectionString: string) (userId: string, urls: string array) =
        async {
            let existingUrls = (DataAccess.getRSSUrls connectionString userId) |> List.toArray

            let newUrls =
                urls |> Array.filter (fun url -> not <| Array.contains url existingUrls)

            let deletedUrls =
                existingUrls |> Array.filter (fun url -> not <| Array.contains url urls)

            if newUrls.Length <> 0 then
                DataAccess.insertUrls connectionString userId newUrls

            if deletedUrls.Length <> 0 then
                DataAccess.deleteUrls connectionString userId deletedUrls
        }

    let initLogin (connectionString: string) (sessionId: string) : LoginResponse Async =
        async {
            return
                sessionId
                |> DataAccess.getUserSession connectionString
                |> (function
                | None ->
                    let loginError = { LoginError.Message = "Session invalid." }
                    Failed loginError
                | Some(user: User) ->
                    let loginResult =
                        { LoginResult.UserId = user.Id
                          LoginResult.RssUrls = (DataAccess.getRSSUrls connectionString user.Id) |> List.toArray
                          LoginResult.SessionId = sessionId }

                    Success loginResult)
        }

    let rssIndexAction (ctx: HttpContext) =
        task {
            let! rssList = ctx.Request.Query.Item("url").ToArray() |> getRSSList
            return! rssList |> Controller.json ctx
        }

let rpcStore =
    { getRSSList = Handler.getRSSList
      loginOrRegister = (Handler.loginOrRegister connectionString)
      saveRSSUrls = (Handler.saveRSSUrls connectionString)
      initLogin = (Handler.initLogin connectionString) }

let webApp =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.routeBuilder
    |> Remoting.fromValue rpcStore
    |> Remoting.buildHttpHandler

module Router =
    let rssController = controller { index Handler.rssIndexAction }

    let apiRouter = router { forward "/rss" rssController }

    let defaultView =
        router {
            forward "" webApp
            forward "/api" apiRouter
        }

let app =
    application {
        use_static "wwwroot"
        use_router Router.defaultView
        memory_cache
        use_gzip
    }

run app
