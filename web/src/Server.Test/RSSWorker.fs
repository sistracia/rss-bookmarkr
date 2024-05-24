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

let overreactedURL: string = "https://overreacted.io/rss.xml"
let infoqURL: string = "https://feed.infoq.com/"
let stackoverflowURL: string = "https://stackoverflow.blog/feed/"

let rssURLs: string array = [| overreactedURL; infoqURL; stackoverflowURL |]

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
                        let overreactedRSS: RSS = (generateRemoteRSS Day.Today) overreactedURL

                        let overreactedRSSP12: RSS =
                            { overreactedRSS with
                                PublishDate = DateTime.Today.AddHours(12) }

                        let overreactedRSSP1: RSS =
                            { overreactedRSS with
                                PublishDate = DateTime.Today.AddHours(1) }

                        let overreactedRSSP0: RSS =
                            { overreactedRSS with
                                PublishDate = DateTime.Today.AddHours(0) }

                        let overreactedRSSM1: RSS =
                            { overreactedRSS with
                                PublishDate = DateTime.Today.AddHours(-1) }

                        let overreactedRSSM12: RSS =
                            { overreactedRSS with
                                PublishDate = DateTime.Today.AddHours(-12) }

                        let overreactedRSSM23: RSS =
                            { overreactedRSS with
                                PublishDate = DateTime.Today.AddHours(-23) }

                        let overreactedRSSM24: RSS =
                            { overreactedRSS with
                                PublishDate = DateTime.Today.AddHours(-24) }

                        let overreactedRSSM25: RSS =
                            { overreactedRSS with
                                Title = "8"
                                PublishDate = DateTime.Today.AddHours(-25) }

                        let groupedRemoteRSSList: (string * RSS seq) array =
                            [| (overreactedURL,
                                [ overreactedRSSP12
                                  overreactedRSSP1
                                  overreactedRSSP0
                                  overreactedRSSM1
                                  overreactedRSSM12
                                  overreactedRSSM23
                                  overreactedRSSM24
                                  overreactedRSSM25 ]) |]

                        let expected: RSS seq array =
                            [| [ overreactedRSSP12
                                 overreactedRSSP1
                                 overreactedRSSP0
                                 overreactedRSSM1
                                 overreactedRSSM12
                                 overreactedRSSM23 ] |]

                        let actual: RSS seq array =
                            rssProcessingService.FilterNewRSS (DateTime.Today.AddHours(-24.0)) groupedRemoteRSSList

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
                            [ (generateRemoteRSS Day.Yesterday) overreactedURL
                              (generateRemoteRSS Day.Today) overreactedURL
                              (generateRemoteRSS Day.Tomorrow) overreactedURL ]

                        let infoqRSS: RSS seq =
                            [ (generateRemoteRSS Day.Today) infoqURL
                              (generateRemoteRSS Day.Tomorrow) infoqURL ]

                        let stackoverflowRSS: RSS seq =
                            [ (generateRemoteRSS Day.Tomorrow) stackoverflowURL ]

                        let input: RSS seq array = [| overreactedRSS; infoqRSS; stackoverflowRSS |]

                        let expected: RSS seq = Seq.concat [ overreactedRSS; infoqRSS; stackoverflowRSS ]

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
                            [ (generateRemoteRSS Day.Yesterday) overreactedURL
                              (generateRemoteRSS Day.Today) overreactedURL
                              (generateRemoteRSS Day.Tomorrow) overreactedURL ]

                        let infoqRSS: RSS seq =
                            [ (generateRemoteRSS Day.Today) infoqURL
                              (generateRemoteRSS Day.Tomorrow) infoqURL ]

                        let stackoverflowRSS: RSS seq =
                            [ (generateRemoteRSS Day.Tomorrow) stackoverflowURL ]

                        let input: RSS seq array = [| overreactedRSS; infoqRSS; stackoverflowRSS |]

                        let expected: RSS seq =
                            [ (generateRemoteRSS Day.Yesterday) overreactedURL
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

                    ]

          ]
