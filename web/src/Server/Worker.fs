module Worker

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

type SendEmailSubscription(delay: int, rssProcessingService: RSSWorker.IRSSProcessingService, logger: ILogger<unit>) =
    inherit BackgroundService()

    /// Called when the background service needs to run.
    override this.ExecuteAsync(stoppingToken: CancellationToken) =
        task {
            logger.LogInformation "Background service start."
            this.DoWork(stoppingToken) |> Async.AwaitTask |> ignore
        }

    member private _.DoWork(stoppingToken: CancellationToken) : Task =
        task {
            try
                logger.LogInformation "Background service running."

                let mutable isSend: bool = DateTime.Now.Hour = 0

                while true do
                    logger.LogInformation "Background service run."

                    // Process RSS subscription every within range 12 midnight once
                    if DateTime.Now.Hour = 0 && not isSend then
                        do! rssProcessingService.DoWork stoppingToken
                        isSend <- true
                    else if DateTime.Now.Hour <> 0 then
                        isSend <- false

                    do! Task.Delay(delay, stoppingToken)
            with (ex: exn) ->
                logger.LogInformation $"error SendEmailSubscription.DoWork: {ex.Message}"
        }

    /// Called when a background service needs to gracefully shut down.
    override this.StopAsync(stoppingToken: CancellationToken) =
        task {
            logger.LogInformation "Background service shutting down."
            this.StopAsync stoppingToken |> Async.AwaitTask |> ignore
        }
