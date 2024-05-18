module Tests

open System
open Expecto

open Types
open Shared

type MailService(__: Mail.MailSettings) =
    interface Mail.IMailService with
        member ___.SendMail(____: Mail.MailData) = printf "Send email"

type Day =
    | Yesterday = -1
    | Today = 0
    | Tomorrow = 1

let generateDayString (day: Day) : string =
    match day with
    | Day.Yesterday -> "Yesterday "
    | Day.Today -> "Today "
    | Day.Tomorrow -> "Tomorrow "
    | _ -> ""

let generateRemoteRSS (day: Day) (origin: string) : RSS =
    let dayString: string = generateDayString day

    { RSS.Origin = origin
      RSS.Link = sprintf "%sRemote Link" dayString
      RSS.TimeAgo = sprintf "%sRemote TimeAgo" dayString
      RSS.Title = sprintf "%sRemote Title" dayString
      RSS.PublishDate = DateTime.Today.AddDays(float day) }

let generateHistoryRSS (day: Day) (url: string) : RSSHistory =
    { RSSHistory.Url = url
      RSSHistory.LatestUpdated = DateTime.Today.AddDays(float day) }

let overreadtedURL: string = "https://overreacted.io/rss.xml"
let infoqURL: string = "https://feed.infoq.com/"
let stackoverflowURL: string = "https://stackoverflow.blog/feed/"

let rssURLs: string array = [| overreadtedURL; infoqURL; stackoverflowURL |]

let groupedRemoteRSSList =
    rssURLs
    |> Array.map (fun (rssURL: string) ->
        let remoteRSSList: RSS seq =
            [ (generateRemoteRSS Day.Yesterday) rssURL
              (generateRemoteRSS Day.Today) rssURL
              (generateRemoteRSS Day.Tomorrow) rssURL ]

        (rssURL, remoteRSSList))

[<Tests>]
let rssWorkerTests =
    testList
        "RSSProcessingService"
        [ let withRSSProcessingService f () =
              let connectionString: string = "random connection string"
              let publicHost: string = "random public host"
              let mailSettins: Mail.MailSettings = Mail.MailSettings()
              let mailService: Mail.IMailService = MailService(mailSettins)

              let rssProcessingService =
                  RSSWorker.RSSProcessingService(connectionString, publicHost, mailService)

              f rssProcessingService

          yield!
              testFixture
                  withRSSProcessingService
                  [

                    "FilterNewRSS > should be resulting the latest RSS from remote that has latest published date compare to last updated date in RSS history",
                    (fun (rssProcessingService: RSSWorker.RSSProcessingService) ->
                        let hiistoryRSSList: RSSHistory array =
                            [| (generateHistoryRSS Day.Yesterday) overreadtedURL
                               (generateHistoryRSS Day.Today) infoqURL
                               (generateHistoryRSS Day.Tomorrow) stackoverflowURL |]

                        let expectedOverreacted: RSS seq =
                            [ (generateRemoteRSS Day.Yesterday) overreadtedURL
                              (generateRemoteRSS Day.Today) overreadtedURL
                              (generateRemoteRSS Day.Tomorrow) overreadtedURL ]

                        let expectedInfoq: RSS seq =
                            [ (generateRemoteRSS Day.Today) infoqURL
                              (generateRemoteRSS Day.Tomorrow) infoqURL ]

                        let expectedStackoveflow: RSS seq =
                            [ (generateRemoteRSS Day.Tomorrow) stackoverflowURL ]

                        let expected: RSS seq array =
                            [| expectedOverreacted; expectedInfoq; expectedStackoveflow |]

                        let actual: RSS seq array =
                            rssProcessingService.FilterNewRSS hiistoryRSSList groupedRemoteRSSList

                        Expect.isNonEmpty actual "Actual not empty"
                        Expect.equal actual.Length expected.Length "Actual length should be match expected length"

                        for i in 0 .. actual.Length - 1 do
                            let actualItems = actual[i]
                            let expectedItems = expected[i]

                            let actualItemsLength = (actualItems |> Seq.length)
                            let expectedItemsLength = (expectedItems |> Seq.length)

                            Expect.equal actualItemsLength expectedItemsLength (sprintf "Index-%d length equal" i)

                            for j in 0 .. actualItemsLength - 1 do
                                let actualItem = actualItems |> Seq.tryItem j
                                let expectedItem = expectedItems |> Seq.tryItem j

                                Expect.isSome actualItem (sprintf "Index-%d.%d actual item is some" i j)
                                Expect.isSome expectedItem (sprintf "Index-%d.%d expected item is some" i j)

                                Expect.equal
                                    actualItem
                                    expectedItem
                                    (sprintf "Index-%d.%d actual and expected item is equal" i j)

                    )

                    "FlattenNewRSS > should be resulting the flat list version of remote RSS from 2D list of remote RSS",
                    (fun (rssProcessingService: RSSWorker.RSSProcessingService) ->
                        let overreactedRSS: RSS seq =
                            [ (generateRemoteRSS Day.Yesterday) overreadtedURL
                              (generateRemoteRSS Day.Today) overreadtedURL
                              (generateRemoteRSS Day.Tomorrow) overreadtedURL ]

                        let infoqRSS: RSS seq =
                            [ (generateRemoteRSS Day.Today) infoqURL
                              (generateRemoteRSS Day.Tomorrow) infoqURL ]

                        let stackoveflowRSS: RSS seq = [ (generateRemoteRSS Day.Tomorrow) stackoverflowURL ]

                        let input: RSS seq array = [| overreactedRSS; infoqRSS; stackoveflowRSS |]

                        let expected: RSS seq = Seq.concat [ overreactedRSS; infoqRSS; stackoveflowRSS ]

                        let actual: RSS seq = rssProcessingService.FlattenNewRSS input

                        Expect.isNonEmpty actual "Actual not empty"

                        Expect.equal
                            (actual |> Seq.length)
                            (expected |> Seq.length)
                            "Actual length should be match expected length"

                        for i in 0 .. (actual |> Seq.length) - 1 do
                            let actualItem = actual |> Seq.tryItem i
                            let expectedItem = expected |> Seq.tryItem i

                            Expect.isSome actualItem (sprintf "Index-%d actual item is some" i)
                            Expect.isSome expectedItem (sprintf "Index-%d expected item is some" i)

                            Expect.equal
                                actualItem
                                expectedItem
                                (sprintf "Index-%d actual and expected item is equal" i)

                    )

                    "LatestNewRSS > should be pick only head item in inner list of 2D list of remote RSS",
                    (fun (rssProcessingService: RSSWorker.RSSProcessingService) ->
                        let overreactedRSS: RSS seq =
                            [ (generateRemoteRSS Day.Yesterday) overreadtedURL
                              (generateRemoteRSS Day.Today) overreadtedURL
                              (generateRemoteRSS Day.Tomorrow) overreadtedURL ]

                        let infoqRSS: RSS seq =
                            [ (generateRemoteRSS Day.Today) infoqURL
                              (generateRemoteRSS Day.Tomorrow) infoqURL ]

                        let stackoveflowRSS: RSS seq = [ (generateRemoteRSS Day.Tomorrow) stackoverflowURL ]

                        let input: RSS seq array = [| overreactedRSS; infoqRSS; stackoveflowRSS |]

                        let expected: RSS seq =
                            [ (generateRemoteRSS Day.Yesterday) overreadtedURL
                              (generateRemoteRSS Day.Today) infoqURL
                              (generateRemoteRSS Day.Tomorrow) stackoverflowURL ]

                        let actual: RSS seq = rssProcessingService.LatestNewRSS input

                        Expect.isNonEmpty actual "Actual not empty"

                        Expect.equal
                            (actual |> Seq.length)
                            (expected |> Seq.length)
                            "Actual length should be match expected length"

                        for i in 0 .. (actual |> Seq.length) - 1 do
                            let actualItem = actual |> Seq.tryItem i
                            let expectedItem = expected |> Seq.tryItem i

                            Expect.isSome actualItem (sprintf "Index-%d actual item is some" i)
                            Expect.isSome expectedItem (sprintf "Index-%d expected item is some" i)

                            Expect.equal
                                actualItem
                                expectedItem
                                (sprintf "Index-%d actual and expected item is equal" i)

                    )

                    ] ]
