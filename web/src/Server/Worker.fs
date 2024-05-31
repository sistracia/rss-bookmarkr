module Worker

open System
open System.Threading
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

/// Ref: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-8.0&tabs=visual-studio
type SendEmailSubscription(rssProcessingService: RSSWorker.IRSSProcessingService, logger: ILogger<unit>) =
    inherit BackgroundService()

    /// Called when the background service needs to run.
    override __.ExecuteAsync(stoppingToken: CancellationToken) =
        task {
            logger.LogInformation "Timed Hosted Service running."

            let timer: PeriodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1.0))
            let hourSend: int = 21
            let mutable isSend: bool = DateTime.Now.Hour = hourSend

            // Ref: https://stackoverflow.com/questions/73806802/how-to-use-while-loop-in-f-async-expression
            try

                let rec loop () =
                    async {
                        logger.LogInformation "Timed Hosted Service is working"
                        let! (delayTask: bool) = timer.WaitForNextTickAsync(stoppingToken).AsTask() |> Async.AwaitTask

                        if delayTask then
                            // Process RSS subscription every within range 12 midnight once
                            if DateTime.Now.Hour = hourSend && not isSend then
                                do! rssProcessingService.DoWork stoppingToken
                                isSend <- true
                            else if DateTime.Now.Hour <> hourSend then
                                isSend <- false

                            return! loop ()
                    }

                do! Async.StartAsTask(loop ())
            with (ex: exn) ->
                logger.LogInformation $"Timed Hosted Service is stopping: {ex.Message}"
        }
