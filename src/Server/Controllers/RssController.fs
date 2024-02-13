namespace Server.Controllers

open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open System.Xml
open System.ServiceModel.Syndication


type RSS =
    struct
        val Title: string

        val Link: string

        new(title: string, link: string) = { Title = title; Link = link }
    end

[<ApiController>]
[<Route("[controller]")>]
type RssController(logger: ILogger<RssController>) =
    inherit ControllerBase()

    let parseRSSdotnet () =
        use reader = (XmlReader.Create("https://about.gitlab.com/atom.xml"))

        SyndicationFeed.Load(reader).Items
        |> Seq.map (fun item ->
            new RSS(
                item.Title.Text,
                match item.Links |> Seq.tryHead with
                | Some first -> first.Uri.AbsoluteUri
                | None -> "-"
            ))


    [<HttpGet>]
    member _.Get() = async { return parseRSSdotnet () }
