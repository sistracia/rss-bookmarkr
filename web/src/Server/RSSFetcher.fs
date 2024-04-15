module RSSFetcher

open System.Xml
open System.ServiceModel.Syndication

open Shared

let mapSydicatoinItem (originURL: string) (item: SyndicationItem) : RSS =
    { RSS.Origin = originURL
      RSS.Title = item.Title.Text
      RSS.PublishDate = item.PublishDate.DateTime
      RSS.TimeAgo = Time.getTimeAgo item.PublishDate.DateTime
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
