module ViewHandler

open Microsoft.AspNetCore.Http
open Giraffe

open Shared
open Types
open Extensions

let unsubsribePageAction (unsubscribeQueryString: UnsubscribeQueryString) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            do! Handler.unsubscribe ctx.RssDbConnectionString { UnsubscribeReq.Email = unsubscribeQueryString.Email }

            return! htmlView Views.unsubsribePage next ctx
        }
