open System
open System.Xml
open System.Threading
open System.Threading.Tasks
open System.ServiceModel.Syndication
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration
open Saturn
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Npgsql.FSharp

open Shared

let rssDbConnectionStringKey = "RssDb"

/// Ref: https://github.com/CompositionalIT/TodoService/blob/main/src/app/Todo.Api.fs
type HttpContext with

    /// The SQL connection string to the RSS database.
    member this.RssDbConnectionString =
        match this.GetService<IConfiguration>().GetConnectionString rssDbConnectionStringKey with
        | null -> failwith "Missing connection string"
        | v -> v

type User =
    { Id: string
      Username: string
      Password: string
      Email: string }

    member this.IsSubscribing = this.Email <> ""

type RssUrl =
    { Id: string
      Url: string
      UserId: string }

type RssEmailsAggregate = { Url: string; Emails: string }

let connectionString = Environment.GetEnvironmentVariable "DB_CONNECTION_STRING"

/// Ref: https://stackoverflow.com/a/1248/12976234
/// And thanks to ChatGPT for convert the code for me :)
module TimeAgo =

    let SECOND: int = 1
    let MINUTE: int = 60 * SECOND
    let HOUR: int = 60 * MINUTE
    let DAY: int = 24 * HOUR
    let MONTH: int = 30 * DAY

    let getTimeAgo (inputDate: DateTime) : string =
        let ts: TimeSpan = TimeSpan(DateTime.UtcNow.Ticks - inputDate.Ticks)
        let delta: float = Math.Abs(ts.TotalSeconds)

        if delta < 1.0 * float MINUTE then
            if ts.Seconds = 1 then
                "one second ago"
            else
                sprintf "%d seconds ago" ts.Seconds
        elif delta < 2.0 * float MINUTE then
            "a minute ago"
        elif delta < 45.0 * float MINUTE then
            sprintf "%d minutes ago" ts.Minutes
        elif delta < 90.0 * float MINUTE then
            "an hour ago"
        elif delta < 24.0 * float HOUR then
            sprintf "%d hours ago" ts.Hours
        elif delta < 48.0 * float HOUR then
            "yesterday"
        elif delta < 30.0 * float DAY then
            sprintf "%d days ago" ts.Days
        elif delta < 12.0 * float MONTH then
            let months = int (float ts.Days / 30.0)

            if months <= 1 then
                "one month ago"
            else
                sprintf "%d months ago" months
        else
            let years = int (float ts.Days / 365.0)

            if years <= 1 then
                "one year ago"
            else
                sprintf "%d years ago" years

module RSS =

    let parseRSS (url: string) =
        async {
            use reader = XmlReader.Create url

            return
                (SyndicationFeed.Load reader).Items
                |> Seq.map (fun item ->
                    { RSS.Title = item.Title.Text
                      RSS.LastUpdatedTime = item.LastUpdatedTime.DateTime
                      RSS.TimeAgo = TimeAgo.getTimeAgo item.LastUpdatedTime.DateTime
                      RSS.Link =
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
            { User.Id = read.string "id"
              User.Username = read.text "username"
              User.Password = read.text "password"
              User.Email = "" })
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
            { User.Id = read.string "user_id"
              User.Username = read.text "user_username"
              User.Password = read.text "user_password"
              User.Email = "" })
        |> List.tryHead

    let aggreateRssEmails (cancellationToken: CancellationToken) (connectionString: string) =
        connectionString
        |> Sql.connect
        |> Sql.cancellationToken cancellationToken
        |> Sql.query
            "SELECT ru.url, STRING_AGG(u.email, ', ') AS emails FROM rss_urls ru LEFT JOIN users u ON u.id = ru.user_id GROUP BY url"
        |> Sql.execute (fun read ->
            { RssEmailsAggregate.Url = read.string "url"
              RssEmailsAggregate.Emails = read.text "emails" })

module Worker =
    /// A full background service using a dedicated type.
    /// Ref: https://github.com/CompositionalIT/background-services
    type SendEmailSubscription(configuration: IConfiguration, logger: ILogger<unit>) =
        inherit BackgroundService()

        /// Called when the background service needs to run.
        override this.ExecuteAsync(stoppingToken: CancellationToken) =
            task {
                logger.LogInformation "Background service start."
                this.DoWork(stoppingToken) |> Async.AwaitTask |> ignore
            }

        member private __.DoWork(stoppingToken: CancellationToken) : Task =
            task {
                while true do
                    logger.LogInformation "Background service running."

                    let rssEmails =
                        DataAccess.aggreateRssEmails
                            stoppingToken
                            (configuration.GetConnectionString rssDbConnectionStringKey)

                    rssEmails |> List.iter ((fun item -> printf $"{item.Url}"))

                    do! Task.Delay(15000, stoppingToken)
            }

        /// Called when a background service needs to gracefully shut down.
        override this.StopAsync(stoppingToken: CancellationToken) =
            task {
                logger.LogInformation "Background service shutting down."
                this.StopAsync stoppingToken |> Async.AwaitTask |> ignore
            }

module Handler =
    let getRSSList (urls: string array) =
        async {
            let! rssList = urls |> Seq.map (fun url -> RSS.parseRSS url |> Async.Catch) |> Async.Parallel

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
              LoginResult.SessionId = sessionId
              LoginResult.IsSubscribing = false }

        Success loginResult

    let login (connectionString: string) (sessionId: string) (loginForm: LoginForm) (user: User) : LoginResponse =
        if user.Password = loginForm.Password then
            let loginResult =
                { LoginResult.UserId = user.Id
                  LoginResult.RssUrls = (DataAccess.getRSSUrls connectionString user.Id) |> List.toArray
                  LoginResult.SessionId = sessionId
                  LoginResult.IsSubscribing = user.IsSubscribing }

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
                          LoginResult.SessionId = sessionId
                          LoginResult.IsSubscribing = user.IsSubscribing }

                    Success loginResult)
        }

    let rssIndexAction (ctx: HttpContext) =
        task {
            let! rssList = ctx.Request.Query.Item("url").ToArray() |> getRSSList
            return! rssList |> Controller.json ctx
        }

let rpcStore (ctx: HttpContext) =
    { IRPCStore.getRSSList = Handler.getRSSList
      IRPCStore.loginOrRegister = (Handler.loginOrRegister ctx.RssDbConnectionString)
      IRPCStore.saveRSSUrls = (Handler.saveRSSUrls ctx.RssDbConnectionString)
      IRPCStore.initLogin = (Handler.initLogin ctx.RssDbConnectionString) }

let webApp =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.routeBuilder
    |> Remoting.fromContext rpcStore
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
        service_config (fun (s: IServiceCollection) -> s.AddHostedService<Worker.SendEmailSubscription>())
        use_router Router.defaultView
        memory_cache
        use_gzip
    }

run app
