module ApiHandler

open Microsoft.AspNetCore.Http
open Giraffe

open Shared
open Types
open Extensions

let rssListAction (rssQueryString: RSSQueryString) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! (rssList: RSS seq) = rssQueryString.Url |> Handler.getRSSList
            return! json rssList next ctx
        }

let loginOrRegisterAction (loginForm: LoginForm) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! (loginResponse: LoginResponse) = (Handler.loginOrRegister ctx.RssDbConnectionString loginForm)
            return! json loginResponse next ctx
        }

let saveRSSUrlsAction (saveRSSUrlReq: SaveRSSUrlReq) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            do! (Handler.saveRSSUrls ctx.RssDbConnectionString saveRSSUrlReq)
            return! Successful.OK "ok" next ctx
        }

let initLoginAction (initLoginReq: InitLoginReq) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! (loginResponse: LoginResponse) = (Handler.initLogin ctx.RssDbConnectionString initLoginReq)
            return! json loginResponse next ctx
        }

let subscribeAction (subscribeReq: SubscribeReq) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            do! (Handler.subscribe ctx.RssDbConnectionString subscribeReq)
            return! Successful.OK "ok" next ctx
        }

let unsubscribeAction (unsubscribeReq: UnsubscribeReq) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            do! (Handler.unsubscribe ctx.RssDbConnectionString unsubscribeReq)
            return! Successful.OK "ok" next ctx
        }
