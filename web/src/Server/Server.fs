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

let getUser (loginForm: LoginForm) =
    connectionString
    |> Sql.connect
    |> Sql.query "SELECT id, username, password FROM users WHERE username = @username"
    |> Sql.parameters [ "@username", Sql.string loginForm.Username ]
    |> Sql.execute (fun read ->
        { Id = read.string "id"
          Username = read.text "username"
          Password = read.text "password" })
    |> List.tryHead

let insertUser (loginForm: LoginForm) =
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

let getRSSUrls (userId: string) =
    connectionString
    |> Sql.connect
    |> Sql.query "SELECT url FROM rss_urls WHERE user_id = @user_id"
    |> Sql.parameters [ "@user_id", Sql.string userId ]
    |> Sql.execute (fun read -> read.text "url")

let insertUrls (userId: string) (urls: string array) =
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

let loginOrRegister loginForm =
    async {
        return
            match getUser loginForm with
            | Some user ->
                if user.Password = loginForm.Password then
                    Some user.Id
                else
                    None
            | None -> Some(insertUser loginForm)
    }

let saveRSSUrls (userId: string, urls: string array) =
    async {
        let existingUrls = (getRSSUrls userId) |> List.toArray

        let newUrls =
            urls |> Array.filter (fun url -> not <| Array.contains url existingUrls)

        if newUrls.Length = 0 then
            ()

        insertUrls userId newUrls
    }

let rpcStore =
    { getRSSList = getRSSList
      loginOrRegister = loginOrRegister
      saveRSSUrls = saveRSSUrls }

let webApp =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.routeBuilder
    |> Remoting.fromValue rpcStore
    |> Remoting.buildHttpHandler

let rssIndexAction (ctx: HttpContext) =
    task {
        let! rssList = ctx.Request.Query.Item("url").ToArray() |> getRSSList
        return! rssList |> Controller.json ctx
    }

let rssController = controller { index rssIndexAction }

let apiRouter = router { forward "/rss" rssController }

let defaultView =
    router {
        forward "" webApp
        forward "/api" apiRouter
    }

let app =
    application {
        use_router defaultView
        memory_cache
        use_gzip
    }

run app
