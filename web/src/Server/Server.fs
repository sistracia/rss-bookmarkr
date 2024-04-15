open System
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration
open Saturn
open Giraffe
open Fable.Remoting.Server
open Fable.Remoting.Giraffe

open Shared
open Types
open Extensions

/// Ref: https://github.com/giraffe-fsharp/Giraffe/issues/323#issuecomment-777622090
let tryBindJson<'T> (parsingErrorHandler: string -> HttpHandler) (successHandler: 'T -> HttpHandler) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            try
                let! model = ctx.BindJsonAsync<'T>()
                return! successHandler model next ctx
            with (ex: exn) ->
                return! parsingErrorHandler ex.Message next ctx
        }

/// A full background service using a dedicated type.
/// Ref: https://github.com/CompositionalIT/background-services
type ApplicationBuilder with

    /// Custom keyword to more easily add a background worker to ASP .NET
    [<CustomOperation "background_service">]
    member _.BackgroundService(state: ApplicationState, serviceBuilder: Func<IServiceProvider, 'a>) =
        { state with
            ServicesConfig =
                (fun (svcCollection: IServiceCollection) -> svcCollection.AddHostedService serviceBuilder)
                :: state.ServicesConfig }

    member this.BackgroundService(state: ApplicationState, backgroundSvc) =
        let worker (serviceProvider: IServiceProvider) =
            { new BackgroundService() with
                member _.ExecuteAsync(cancellationToken: Threading.CancellationToken) =
                    backgroundSvc serviceProvider cancellationToken }

        this.BackgroundService(state, worker)

let publicHost: string = Environment.GetEnvironmentVariable "PUBLIC_HOST"

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
    let apiRouter =
        router {
            get "/rss" (tryBindQuery<RSSQueryString> RequestErrors.BAD_REQUEST None (ApiHandler.rssListAction))

            post
                "/login"
                (tryBindJson<LoginForm> RequestErrors.BAD_REQUEST (validateModel ApiHandler.loginOrRegisterAction))

            post
                "/save-urls"
                (tryBindJson<SaveRSSUrlReq> RequestErrors.BAD_REQUEST (validateModel ApiHandler.saveRSSUrlsAction))

            post
                "/init-login"
                (tryBindJson<InitLoginReq> RequestErrors.BAD_REQUEST (validateModel ApiHandler.initLoginAction))

            post
                "/subscribe"
                (tryBindJson<SubscribeReq> RequestErrors.BAD_REQUEST (validateModel ApiHandler.subscribeAction))

            post
                "/unsubscribe"
                (tryBindJson<UnsubscribeReq> RequestErrors.BAD_REQUEST (validateModel ApiHandler.unsubscribeAction))
        }

    let defaultView =
        router {
            forward "" webApp
            forward "/api" apiRouter

            get
                "/unsubscribe"
                (tryBindQuery<UnsubscribeQueryString> RequestErrors.BAD_REQUEST None (ViewHandler.unsubsribePageAction))
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

            let mailService = Mail.MailService(mailSettings)

            let rssProcessingService =
                RSSWorker.RSSProcessingService(connectionString, publicHost, mailService)

            let minutesInMS = 1000 * 60

            new Worker.SendEmailSubscription(minutesInMS, rssProcessingService, logger))

        use_router Router.defaultView
        memory_cache
        use_gzip
    }

run app
