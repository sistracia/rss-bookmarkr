open Microsoft.AspNetCore.Http
open Saturn
open System
open System.Xml
open System.ServiceModel.Syndication

type RSS =
    struct
        val Title: string

        val LastUpdatedTime: DateTime

        val Link: string

        new(title: string, lastUpdatedTime: DateTime, link: string) =
            { Title = title
              LastUpdatedTime = lastUpdatedTime
              Link = link }
    end


let parseRSS (url: string) =
    async {
        use reader = XmlReader.Create url

        return
            (SyndicationFeed.Load reader).Items
            |> Seq.map (fun item ->
                new RSS(
                    item.Title.Text,
                    item.LastUpdatedTime.DateTime,
                    match item.Links |> Seq.tryHead with
                    | Some first -> first.Uri.AbsoluteUri
                    | None -> "-"
                ))
    }

let rssIndexAction (ctx: HttpContext) =
    task {
        let! rssList = ctx.Request.Query.Item("url").ToArray() |> Seq.map parseRSS |> Async.Parallel

        return!
            rssList
            |> Array.reduce (fun acc elem -> Seq.concat [ acc; elem ])
            |> Seq.sortByDescending (fun rss -> rss.LastUpdatedTime)
            |> Controller.json ctx
    }

let rssController = controller { index rssIndexAction }

let apiRouter = router { forward "/rss" rssController }

let defaultView = router { forward "/api" apiRouter }

let app = application { use_router defaultView }

run app
