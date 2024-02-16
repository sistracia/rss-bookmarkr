open Microsoft.AspNetCore.Http
open Saturn
open System.Xml
open System.ServiceModel.Syndication
open Fable.Remoting.Server
open Fable.Remoting.Giraffe

open Shared

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

let getRSSList (urls: string array) =
    async {
        let! rssList = urls |> Seq.map parseRSS |> Async.Parallel

        return
            rssList
            |> Seq.fold (fun acc elem -> Seq.concat [ acc; elem ]) []
            |> Seq.sortByDescending (fun rss -> rss.LastUpdatedTime)
    }

let rssStore: IRSSStore = { getRSSList = getRSSList }

let webApp =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.routeBuilder
    |> Remoting.fromValue rssStore
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
