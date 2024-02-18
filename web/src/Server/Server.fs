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

let rpcStore =
    { getRSSList = getRSSList
      loginOrRegister = loginOrRegister }

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
