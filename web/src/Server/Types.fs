module Types

open System
open Giraffe

[<CLIMutable>]
type RSSQueryString =
    { Url: string array }

    override this.ToString() =
        sprintf "Url: %s" (String.concat ", " this.Url)

    member _.HasErrors() = None

    interface IModelValidation<RSSQueryString> with
        member this.Validate() =
            match this.HasErrors() with
            | Some msg -> Error(RequestErrors.badRequest (text msg))
            | None -> Ok this

[<CLIMutable>]
type UnsubscribeQueryString =
    { Email: string }

    override this.ToString() = sprintf "Email: %s" this.Email

    member this.HasErrors() =
        if this.Email.Length <= 0 then
            Some "Email is required."
        else
            None

    interface IModelValidation<UnsubscribeQueryString> with
        member this.Validate() =
            match this.HasErrors() with
            | Some msg -> Error(RequestErrors.badRequest (text msg))
            | None -> Ok this

type User =
    { Id: string
      Username: string
      Password: string
      Email: string option }

type RSSUrl =
    { Id: string
      Url: string
      UserId: string }

type RSSHistory =
    { Url: string; LatestUpdated: DateTime }

type RSSEmailsAggregate =
    { Email: string
      HistoryPairs: RSSHistory option array }

type MailSettings() =
    static member SettingName = "MailSettings"
    member val Server: string = "" with get, set
    member val Port: int = 0 with get, set
    member val SenderName: string = "" with get, set
    member val SenderEmail: string = "" with get, set
    member val UserName: string = "" with get, set
    member val Password: string = "" with get, set
