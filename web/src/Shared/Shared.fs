namespace Shared

open System
open Giraffe

[<Struct>]
[<CLIMutable>]
type InitLoginReq =
    { SessionId: string }

    override this.ToString() = sprintf "SessionId: %s" this.SessionId

    member this.HasErrors() =
        if this.SessionId.Length <= 0 then
            Some "UserId is required."
        else
            None

    interface IModelValidation<InitLoginReq> with
        member this.Validate() =
            match this.HasErrors() with
            | Some msg -> Error(RequestErrors.badRequest (text msg))
            | None -> Ok this

[<Struct>]
[<CLIMutable>]
type LoginReq =
    { Username: string
      Password: string }

    override this.ToString() =
        sprintf "Username: %s, Password: %s" this.Username this.Password

    member this.HasErrors() =
        if this.Username.Length <= 0 then
            Some "Username is required."
        else if this.Password.Length <= 0 then
            Some "Password is required."
        else
            None

    interface IModelValidation<LoginReq> with
        member this.Validate() =
            match this.HasErrors() with
            | Some msg -> Error(RequestErrors.badRequest (text msg))
            | None -> Ok this

[<Struct>]
type LoginSuccess =
    { UserId: string
      RssUrls: string array
      SessionId: string
      Email: string }

[<Struct>]
type LoginFailed = { Message: string }

type LoginResponse =
    | Success of result: LoginSuccess
    | Failed of error: LoginFailed

[<Struct>]
type RSS =
    { Origin: string
      Title: string
      PublishDate: DateTime
      TimeAgo: string
      Link: string }

    member this.OriginHost = (Uri this.Origin).Host

    member this.OriginHostUrl =
        let uri = (Uri this.Origin)
        sprintf $"{uri.Scheme}://{uri.Host}"

[<Struct>]
[<CLIMutable>]
type SaveRSSUrlReq =
    { UserId: string
      Urls: string array }

    override this.ToString() =
        sprintf "UserId: %s, Urls: %s" this.UserId (String.concat ", " this.Urls)

    member this.HasErrors() =
        if this.UserId.Length <= 0 then
            Some "UserId is required."
        else
            None

    interface IModelValidation<SaveRSSUrlReq> with
        member this.Validate() =
            match this.HasErrors() with
            | Some msg -> Error(RequestErrors.badRequest (text msg))
            | None -> Ok this

[<Struct>]
[<CLIMutable>]
type SubscribeReq =
    { UserId: string
      Email: string }

    override this.ToString() =
        sprintf "UserId: %s, Email: %s" this.UserId this.Email

    member this.HasErrors() =
        if this.UserId.Length <= 0 then
            Some "UserId is required."
        else if this.Email.Length <= 0 then
            Some "Email is required."
        else
            None

    interface IModelValidation<SubscribeReq> with
        member this.Validate() =
            match this.HasErrors() with
            | Some msg -> Error(RequestErrors.badRequest (text msg))
            | None -> Ok this

[<Struct>]
[<CLIMutable>]
type UnsubscribeReq =
    { Email: string }

    override this.ToString() = sprintf "Email: %s" this.Email

    member this.HasErrors() =
        if this.Email.Length <= 0 then
            Some "Email is required."
        else
            None

    interface IModelValidation<UnsubscribeReq> with
        member this.Validate() =
            match this.HasErrors() with
            | Some msg -> Error(RequestErrors.badRequest (text msg))
            | None -> Ok this
type IRPCStore =
    { initLogin: InitLoginReq -> LoginResponse Async
      loginOrRegister: LoginReq -> LoginResponse Async
      getRSSList: string array -> RSS seq Async
      saveRSSUrls: SaveRSSUrlReq -> unit Async
      subscribe: SubscribeReq -> unit Async
      unsubscribe: UnsubscribeReq -> unit Async }

module Route =
    let routeBuilder (typeName: string) (methodName: string) =
        sprintf "/rpc/%s/%s" typeName methodName
