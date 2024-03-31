open System
open System.IO
open System.Xml
open System.Threading
open System.Threading.Tasks
open System.ServiceModel.Syndication
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Options
open MimeKit
open MailKit.Net.Smtp
open Saturn
open Giraffe.Core
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Npgsql.FSharp

open Shared

let rssDbConnectionStringKey = "RssDb"


/// A full background service using a dedicated type.
/// Ref: https://github.com/CompositionalIT/background-services
type ApplicationBuilder with

    /// Custom keyword to more easily add a background worker to ASP .NET
    [<CustomOperation "background_service">]
    member _.BackgroundService(state: ApplicationState, serviceBuilder) =
        { state with
            ServicesConfig =
                (fun svcCollection -> svcCollection.AddHostedService serviceBuilder)
                :: state.ServicesConfig }

    member this.BackgroundService(state: ApplicationState, backgroundSvc) =
        let worker serviceProvider =
            { new BackgroundService() with
                member _.ExecuteAsync cancellationToken =
                    backgroundSvc serviceProvider cancellationToken }

        this.BackgroundService(state, worker)

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

type RSSUrl =
    { Id: string
      Url: string
      UserId: string }

type RSSHistory = { Url: string; LatestTitle: string }

type RSSHistoryPair =
    { Url: string
      LatestTitle: string
      LatestUpdated: DateTime }

type RSSEmailsAggregate =
    { Email: string
      HistoryPairs: RSSHistoryPair option array }

type MailSettings() =
    static member SettingName = "MailSettings"
    member val Server: string = "" with get, set
    member val Port: int = 0 with get, set
    member val SenderName: string = "" with get, set
    member val SenderEmail: string = "" with get, set
    member val UserName: string = "" with get, set
    member val Password: string = "" with get, set

let publicHost = Environment.GetEnvironmentVariable "PUBLIC_HOST"

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

    let mapSydicatoinItem (originURL: string) (item: SyndicationItem) : RSS =
        { RSS.Origin = originURL
          RSS.Title = item.Title.Text
          RSS.PublishDate = item.PublishDate.DateTime
          RSS.TimeAgo = TimeAgo.getTimeAgo item.PublishDate.DateTime
          RSS.Link =
            match item.Links |> Seq.tryHead with
            | Some first -> first.Uri.AbsoluteUri
            | None -> "-" }

    let getRSSItems (url: string) =
        use reader = XmlReader.Create url
        (SyndicationFeed.Load reader).Items

    let parseRSSItems (url: string) : Async<RSS seq> =
        async { return url |> getRSSItems |> Seq.map (mapSydicatoinItem url) }

    let parseRSS (url: string) : Async<RSS seq> =
        async {
            let! rssList = parseRSSItems url |> Async.Catch

            return
                match rssList with
                | Choice1Of2 rss -> rss
                | Choice2Of2 _ -> Seq.empty
        }

    let parseRSSList (urls: string array) : Async<RSS seq array> =
        async { return! urls |> Seq.map parseRSS |> Async.Parallel }


module DataAccess =

    let getUser (connectionString: string) (loginForm: LoginForm) =
        connectionString
        |> Sql.connect
        |> Sql.query "SELECT id, username, password, email FROM users WHERE username = @username"
        |> Sql.parameters [ "@username", Sql.string loginForm.Username ]
        |> Sql.execute (fun read ->
            { User.Id = read.string "id"
              User.Username = read.text "username"
              User.Password = read.text "password"
              User.Email = read.text "email" })
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
                    |> Array.map (fun (url: string) ->
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

    let getUserSession (connectionString: string) (sessionId: string) =
        connectionString
        |> Sql.connect
        |> Sql.query
            """SELECT
                        u.id AS user_id,
                        u.username AS user_username,
                        u.password AS user_password,
                        u.email AS user_email
                    FROM users u
                    LEFT JOIN sessions s
                        ON s.user_id = u.id
                    WHERE s.id = @session_id"""
        |> Sql.parameters [ "@session_id", Sql.string sessionId ]
        |> Sql.execute (fun read ->
            { User.Id = read.string "user_id"
              User.Username = read.text "user_username"
              User.Password = read.text "user_password"
              User.Email = read.text "user_email" })
        |> List.tryHead

    let aggreateRssEmails (cancellationToken: CancellationToken) (connectionString: string) =
        connectionString
        |> Sql.connect
        |> Sql.cancellationToken cancellationToken
        |> Sql.query
            """SELECT
                        u.email,
                        ARRAY_AGG(ru.url || '|' || rh.latest_title || '|' || rh.latest_updated) AS history_pairs
                    FROM
                        users u
                    JOIN
                        rss_urls ru ON u.id = ru.user_id 
                    JOIN
                        rss_histories rh ON ru .url = rh.url
                    GROUP BY
                        u.email;"""
        |> Sql.execute (fun read ->
            { RSSEmailsAggregate.Email = read.text "email"
              RSSEmailsAggregate.HistoryPairs =
                (read.stringArray "history_pairs")
                |> Array.map (fun (pair: string) ->
                    match pair.Split("|") with
                    | [| url: string; latestTitle: string; latestUpdated: string |] ->
                        Some
                            { RSSHistoryPair.Url = url
                              RSSHistoryPair.LatestTitle = latestTitle
                              RSSHistoryPair.LatestUpdated = DateTime.Parse latestUpdated }
                    | _ -> None) })

    let unsetUserEmail (connectionString: string) (email: string) =
        connectionString
        |> Sql.connect
        |> Sql.query "UPDATE users SET email = '' WHERE email = @email"
        |> Sql.parameters [ "@email", Sql.text email ]
        |> Sql.executeNonQuery
        |> ignore

    let setUserEmail (connectionString: string) (userId: string, email: string) (newRSSHistories: RSSHistory seq) =
        connectionString
        |> Sql.connect
        |> Sql.executeTransaction
            [ "UPDATE users SET email = @email WHERE id = @id", [ [ "@id", Sql.text userId; "@email", Sql.text email ] ]
              """INSERT INTO rss_histories (id, url, latest_title, latest_updated) 
                            VALUES (@id, @url, @latest_title, @latest_updated)
                            ON CONFLICT (url) DO NOTHING""",
              [ yield!
                    newRSSHistories
                    |> Seq.map (fun (rss: RSSHistory) ->
                        [ "@id", Sql.text (Guid.NewGuid().ToString())
                          "@url", Sql.text rss.Url
                          "@latest_title", Sql.text rss.LatestTitle
                          "@latest_updated", Sql.date DateTime.Now ]) ] ]
        |> ignore

    // Upsert the RSS histories
    let renewRSSHistories
        (cancellationToken: CancellationToken)
        (connectionString: string)
        (newRSSHistories: RSSHistory seq)
        =
        connectionString
        |> Sql.connect
        |> Sql.cancellationToken cancellationToken
        |> Sql.executeTransaction
            [ """INSERT INTO rss_histories (id, url, latest_title, latest_updated) 
                            VALUES (@id, @url, @latest_title, @latest_updated)
                            ON CONFLICT (url)
                                DO UPDATE 
                                    SET latest_title=EXCLUDED.latest_title, latest_updated=EXCLUDED.latest_updated""",
              [ yield!
                    newRSSHistories
                    |> Seq.map (fun (rss: RSSHistory) ->
                        [ "@id", Sql.text (Guid.NewGuid().ToString())
                          "@url", Sql.text rss.Url
                          "@latest_title", Sql.text rss.LatestTitle
                          "@latest_updated", Sql.date DateTime.Now ]) ] ]
        |> ignore

/// Ref: https://mailtrap.io/blog/asp-net-core-send-email/
module Mail =
    type MailRecipient =
        { EmailToId: string
          EmailToName: string }

    type MailData =
        { EmailRecipient: MailRecipient
          EmailSubject: string
          EmailTextBody: string
          EmailHtmlBody: string }

    type IMailService =
        abstract member SendMail: mailData: MailData -> unit

    type MailService(mailSettings: MailSettings, logger: ILogger<unit>) =

        interface IMailService with
            member __.SendMail(mailData: MailData) =
                use emailMessage = new MimeMessage()
                let emailFrom = MailboxAddress(mailSettings.SenderName, mailSettings.SenderEmail)
                emailMessage.From.Add(emailFrom)

                let emailTo =
                    MailboxAddress(mailData.EmailRecipient.EmailToName, mailData.EmailRecipient.EmailToId)

                emailMessage.To.Add(emailTo)

                emailMessage.Subject <- mailData.EmailSubject
                let emailBodyBuilder = BodyBuilder()
                emailBodyBuilder.HtmlBody <- mailData.EmailHtmlBody
                emailBodyBuilder.TextBody <- mailData.EmailTextBody

                emailMessage.Body <- emailBodyBuilder.ToMessageBody()

                use mailClient = new SmtpClient()

                mailClient.Connect(
                    mailSettings.Server,
                    mailSettings.Port,
                    MailKit.Security.SecureSocketOptions.StartTls
                )

                mailClient.Authenticate(mailSettings.UserName, mailSettings.Password)
                mailClient.Send(emailMessage) |> ignore
                mailClient.Disconnect(true)


module RSSWorker =

    type IRSSProcessingService =
        abstract member DoWork: stoppingToken: CancellationToken -> Task

    type RSSProcessingService
        (connectionString: string, publicHost: string, mailService: Mail.IMailService, logger: ILogger<unit>) =

        member private __.GetRSSAggregate
            (connectionString: string)
            (stoppingToken: CancellationToken)
            : RSSEmailsAggregate list =
            DataAccess.aggreateRssEmails stoppingToken connectionString

        member private __.GetLatestRemoteRSS(historyPairs: RSSHistoryPair array) =
            async {
                return!
                    historyPairs
                    |> Array.map (fun (historyPair: RSSHistoryPair) ->
                        // Transform to async function that return tuple of url and RSS list
                        async {
                            let! (remoteRSSList: RSS seq) = RSS.parseRSS historyPair.Url
                            return (historyPair.Url, remoteRSSList)
                        })
                    |> Async.Parallel
            }

        /// New RSS determined by latest title not same as stored in database.
        ///
        /// In-case the stored latest title is deleted from remote URL by the author,
        /// make sure the latest item published date from remote URL is not before
        /// the latest update stored in database.
        member private __.FilterNewRemoteRSS (historyPair: RSSHistoryPair) (remoteRSS: RSS) : bool =
            historyPair.LatestTitle <> remoteRSS.Title
            && DateTime.Compare(remoteRSS.PublishDate, historyPair.LatestUpdated) >= 0

        /// Get new RSS from remote URL and compare with RSS history in database
        /// to get detect new publised RSS from remote URL
        member private this.FilterNewRSS
            (historyPairs: RSSHistoryPair array)
            (remoteRSSList: (string * RSS seq) array)
            : RSS seq =
            // Create map used for value lookup
            let remoteRSSListMap: Map<string, RSS seq> = remoteRSSList |> Map.ofArray

            historyPairs
            |> Array.map (fun (historyPair: RSSHistoryPair) ->
                remoteRSSListMap.Item historyPair.Url
                |> Seq.filter (this.FilterNewRemoteRSS historyPair))
            |> Array.fold (fun (acc: RSS seq) (elem: RSS seq) -> Seq.concat [ acc; elem ]) [] // Flatten the RSS list

        member private __.StoreRemoteRSS
            (connectionString: string)
            (stoppingToken: CancellationToken)
            (remoteRSSList: RSS seq)
            =
            remoteRSSList
            |> Seq.map (fun (remoteRSS: RSS) ->
                { RSSHistory.Url = remoteRSS.Origin
                  RSSHistory.LatestTitle = remoteRSS.Title })
            |> DataAccess.renewRSSHistories stoppingToken connectionString

        member private __.CreateEmailRecipient(recipient: string) : Mail.MailRecipient =
            { Mail.MailRecipient.EmailToId = recipient
              Mail.MailRecipient.EmailToName =
                match recipient.Split "@" |> Array.tryHead with
                | None -> recipient
                | Some(username: string) -> username }

        member private __.CreateEmailTextBody(newRSSList: RSS seq) : string =
            newRSSList |> Seq.map (fun (rss: RSS) -> rss.Title) |> String.concat ", "

        member private __.CreateEmailHtmlBody (newRSSList: RSS seq) (recipientEmail: string) : string =
            let templateFilePath =
                Directory.GetCurrentDirectory() + "/Templates/email-notification.html"

            let emailTemplateText = File.ReadAllText(templateFilePath)

            let itemFilePath =
                Directory.GetCurrentDirectory() + "/Templates/email-notification-item.html"

            let itemText = File.ReadAllText(itemFilePath)

            let emailBody =
                newRSSList
                |> Seq.map (fun (rss: RSS) ->
                    String.Format(itemText, rss.Link, rss.Title, rss.OriginHostUrl, rss.OriginHost))
                |> String.concat ""

            emailTemplateText
                .Replace("{0}", emailBody)
                .Replace("{1}", publicHost)
                .Replace("{2}", recipientEmail)

        member private __.SendEmail (recipient: Mail.MailRecipient) (htmlBody: string) (textBody: string) =
            let mailData =
                { Mail.MailData.EmailTextBody = textBody
                  Mail.MailData.EmailHtmlBody = htmlBody
                  Mail.MailData.EmailSubject = "New RSS Release!"
                  Mail.MailData.EmailRecipient = recipient }

            mailService.SendMail mailData

        member private this.ProceedSubscriber(rssAggregate: RSSEmailsAggregate) =
            async {
                let rssHistoryPairs: RSSHistoryPair array =
                    rssAggregate.HistoryPairs |> Array.choose id

                let! (rssList: (string * RSS seq) array) = this.GetLatestRemoteRSS rssHistoryPairs
                let newRSS: RSS seq = this.FilterNewRSS rssHistoryPairs rssList

                if (newRSS |> Seq.length) = 0 then
                    return None
                else
                    this.SendEmail
                        (this.CreateEmailRecipient rssAggregate.Email)
                        (this.CreateEmailHtmlBody newRSS rssAggregate.Email)
                        (this.CreateEmailTextBody newRSS)

                    return Some newRSS
            }

        interface IRSSProcessingService with

            member this.DoWork(stoppingToken: CancellationToken) =
                task {
                    try
                        let! (newRSSList: RSS seq option array) =
                            this.GetRSSAggregate connectionString stoppingToken
                            |> List.map (this.ProceedSubscriber)
                            |> Async.Parallel

                        newRSSList
                        |> Seq.choose id // Remove `None` value
                        |> Seq.fold (fun (acc: RSS seq) (elem: RSS seq) -> Seq.concat [ acc; elem ]) [] // Flatten the RSS list
                        |> Seq.map (fun (rss: RSS) -> (rss.Origin, rss)) // Transform to key-value pair to be able convert to Map
                        |> Map.ofSeq // Convert to Map to remove duplicate item
                        |> Map.toSeq // Convert back to `Seq`
                        |> Seq.map (fun (_, rss: RSS) -> rss) // Get only the value
                        |> (this.StoreRemoteRSS connectionString stoppingToken) // Save to DB
                    with (ex: exn) ->
                        logger.LogInformation $"error RSSProcessingService.DoWork: {ex.Message}"
                }

module Worker =

    type SendEmailSubscription(delay: int, rssProcessingService: RSSWorker.IRSSProcessingService, logger: ILogger<unit>)
        =
        inherit BackgroundService()

        /// Called when the background service needs to run.
        override this.ExecuteAsync(stoppingToken: CancellationToken) =
            task {
                logger.LogInformation "Background service start."
                this.DoWork(stoppingToken) |> Async.AwaitTask |> ignore
            }

        member private _.DoWork(stoppingToken: CancellationToken) : Task =
            task {
                logger.LogInformation "Background service running."

                let mutable isSend = false

                while true do
                    logger.LogInformation "Background service run."

                    // Process RSS subscription every within range 12 midnight once
                    if DateTime.Now.Hour = 0 && not isSend then
                        do! rssProcessingService.DoWork stoppingToken
                        isSend <- true
                    else if DateTime.Now.Hour <> 0 then
                        isSend <- false

                    do! Task.Delay(delay, stoppingToken)
            }

        /// Called when a background service needs to gracefully shut down.
        override this.StopAsync(stoppingToken: CancellationToken) =
            task {
                logger.LogInformation "Background service shutting down."
                this.StopAsync stoppingToken |> Async.AwaitTask |> ignore
            }

module Views =
    open Giraffe.ViewEngine

    let unsubsribePage =
        html
            []
            [ head
                  []
                  [ title [] [ str "Unsubscribe Success" ]
                    style
                        []
                        [ rawText
                              "html { font-family: ui-sans-serif, system-ui, sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol', 'Noto Color Emoji'}" ] ]
              body
                  [ _style
                        "width: 100%;min-height: 100vh;display: flex;flex-direction: column;justify-content: center;align-items: center;gap: 5px" ]
                  [ h1 [ _style "margin: 0; margin-bottom: 15px" ] [ str "Unsubscribe Success" ]
                    p [ _style "margin: 0; margin-bottom: 15px" ] [ str "See you later!" ] ] ]

module Handler =
    let getRSSList (urls: string array) =
        async {
            let! rssList = urls |> RSS.parseRSSList

            return
                rssList
                |> Seq.fold (fun (acc: RSS seq) (elem: RSS seq) -> Seq.concat [ acc; elem ]) []
                |> Seq.sortByDescending _.PublishDate
        }

    let register (connectionString: string) (sessionId: string) (loginForm: LoginForm) : LoginResponse =
        let userId = (DataAccess.insertUser connectionString loginForm)

        let loginResult =
            { LoginResult.UserId = userId
              LoginResult.RssUrls = Array.empty
              LoginResult.SessionId = sessionId
              LoginResult.Email = "" }

        Success loginResult

    let login (connectionString: string) (sessionId: string) (loginForm: LoginForm) (user: User) : LoginResponse =
        if user.Password = loginForm.Password then
            let loginResult =
                { LoginResult.UserId = user.Id
                  LoginResult.RssUrls = (DataAccess.getRSSUrls connectionString user.Id) |> List.toArray
                  LoginResult.SessionId = sessionId
                  LoginResult.Email = user.Email }

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

    let saveRSSUrls (connectionString: string) (userId: string, urls: string array) : unit Async =
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
                          LoginResult.Email = user.Email }

                    Success loginResult)
        }

    let subscribe (connectionString: string) (userId: string, email: string) : unit Async =
        async {
            (DataAccess.getRSSUrls connectionString userId)
            |> List.map (fun (rssURL: string) ->
                { RSSHistory.Url = rssURL
                  RSSHistory.LatestTitle = "" })
            |> (DataAccess.setUserEmail connectionString (userId, email))
            |> ignore
        }

    let unsubscribe (connectionString: string) (email: string) : unit Async =
        async { (DataAccess.unsetUserEmail connectionString email) |> ignore }

    let rssIndexAction (ctx: HttpContext) =
        task {
            let! rssList = ctx.Request.Query.Item("url").ToArray() |> getRSSList
            return! rssList |> Controller.json ctx
        }

    let unsubsribeIndexAction (ctx: HttpContext) =
        task {
            return!
                match ctx.Request.Query.Item("email").ToArray() |> Array.tryHead with
                | None -> Controller.renderHtml ctx Views.unsubsribePage
                | Some(email: string) ->
                    task {
                        do! unsubscribe ctx.RssDbConnectionString email
                        return! Controller.renderHtml ctx Views.unsubsribePage
                    }
        }

let rpcStore (ctx: HttpContext) =
    { IRPCStore.getRSSList = Handler.getRSSList
      IRPCStore.loginOrRegister = (Handler.loginOrRegister ctx.RssDbConnectionString)
      IRPCStore.saveRSSUrls = (Handler.saveRSSUrls ctx.RssDbConnectionString)
      IRPCStore.initLogin = (Handler.initLogin ctx.RssDbConnectionString)
      IRPCStore.subscribe = (Handler.subscribe ctx.RssDbConnectionString)
      IRPCStore.unsubscribe = (Handler.unsubscribe ctx.RssDbConnectionString) }

let webApp =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.routeBuilder
    |> Remoting.fromContext rpcStore
    |> Remoting.buildHttpHandler

module Router =
    let rssController = controller { index Handler.rssIndexAction }
    let unsubscribeController = controller { index Handler.unsubsribeIndexAction }
    let apiRouter = router { forward "/rss" rssController }

    let defaultView =
        router {
            forward "" webApp
            forward "/api" apiRouter
            forward "/unsubscribe" unsubscribeController
        }

let app =
    application {
        use_static "wwwroot"

        background_service (fun (serviceProvider: IServiceProvider) ->
            let configuration = serviceProvider.GetService<IConfiguration>()

            let logger = serviceProvider.GetService<ILogger<unit>>()

            let connectionString = (configuration.GetConnectionString rssDbConnectionStringKey)

            let mailSettings =
                configuration.GetSection(MailSettings.SettingName).Get<MailSettings>()

            let mailService = Mail.MailService(mailSettings, logger)

            let rssProcessingService =
                RSSWorker.RSSProcessingService(connectionString, publicHost, mailService, logger)

            let minutesInMS = 1000 * 60

            new Worker.SendEmailSubscription(minutesInMS, rssProcessingService, logger))

        use_router Router.defaultView
        memory_cache
        use_gzip
    }

run app
