open Microsoft.AspNetCore.Http
open Saturn
open System
open System.Xml
open System.ServiceModel.Syndication

type RSS =
    struct
        val Title: string

        val Link: string

        new(title: string, link: string) = { Title = title; Link = link }
    end


let parseRSS =
    use reader = (XmlReader.Create("https://about.gitlab.com/atom.xml"))

    SyndicationFeed.Load(reader).Items
    |> Seq.map (fun item ->
        new RSS(
            item.Title.Text,
            match item.Links |> Seq.tryHead with
            | Some first -> first.Uri.AbsoluteUri
            | None -> "-"
        ))

let rssIndexAction (ctx: HttpContext) = parseRSS |> Controller.json ctx


let rssController = controller { index rssIndexAction }

let apiRouter = router { forward "/rss" rssController }

let defaultView = router { forward "/api" apiRouter }

let app = application { use_router defaultView }

run app
