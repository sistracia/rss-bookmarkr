module RSSWorker

open System
open System.IO
open System.Threading

open Shared
open Types

type IRSSProcessingService =
    abstract member DoWork: stoppingToken: CancellationToken -> unit Async

type RSSProcessingService(connectionString: string, publicHost: string, mailService: Mail.IMailService) =

    member private __.GetLatestRemoteRSS(urls: string array) =
        async {
            return!
                urls
                |> Array.map (fun (url: string) ->
                    // Transform to async function that return tuple of url and RSS list
                    async {
                        let! (remoteRSSList: RSS seq) = RSSFetcher.parseRSS url
                        return (url, remoteRSSList)
                    })
                |> Async.Parallel
        }

    /// Get new RSS from remote URL and compare with RSS history in database
    /// to get detect new publised RSS from remote URL
    member __.FilterNewRSS (timeLimit: DateTime) (remoteRSSList: (string * RSS seq) array) : RSS seq array =
        // Create map used for value lookup
        let remoteRSSListMap: Map<string, RSS seq> = remoteRSSList |> Map.ofArray

        remoteRSSList
        |> Array.map (fun (remoteRSSList: string * RSS seq) ->
            remoteRSSListMap.Item(fst remoteRSSList)
            // New RSS determined by the publish date within the time limit
            |> Seq.filter (fun (remoteRSS: RSS) -> remoteRSS.PublishDate > timeLimit))

    member __.FlattenNewRSS(recentRemoteRSSList: RSS seq array) : RSS seq =
        recentRemoteRSSList
        |> Array.fold (fun (acc: RSS seq) (elem: RSS seq) -> Seq.concat [ acc; elem ]) [] // Flatten the RSS list

    member __.LatestNewRSS(rssListOfList: RSS seq array) : RSS array =
        rssListOfList
        |> Array.map (fun ((rssList: RSS seq)) -> rssList |> Seq.tryHead)
        |> Array.choose (function
            | Some(rss: RSS) -> Some(rss)
            | _ -> None)
        |> Array.map (fun (rss: RSS) -> rss)

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

    member private this.ProceedSubscriber(rssAggregate: RSSEmailsAggregate) : unit Async =
        async {
            let { UserId = _
                  Email = email: string
                  Urls = urls: string array } =
                rssAggregate

            let! (rssList: (string * RSS seq) array) = this.GetLatestRemoteRSS urls

            let flatNewRSS: RSS seq =
                rssList |> this.FilterNewRSS(DateTime.Now.AddHours(-24.0)) |> this.FlattenNewRSS

            if (flatNewRSS |> Seq.length) <> 0 && email <> "" then
                try
                    this.SendEmail
                        (this.CreateEmailRecipient email)
                        (this.CreateEmailHtmlBody flatNewRSS email)
                        (this.CreateEmailTextBody flatNewRSS)

                // Ignore if there is an error when sending email because invalid email or etc
                with _ ->
                    ()
        }

    interface IRSSProcessingService with

        member this.DoWork(stoppingToken: CancellationToken) =
            async {
                DataAccess.aggreateRssEmails stoppingToken connectionString
                |> List.map (this.ProceedSubscriber)
                |> Async.Parallel
                |> Async.Ignore
                |> Async.RunSynchronously
            }
