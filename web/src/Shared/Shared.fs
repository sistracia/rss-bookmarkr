namespace Shared

open System

[<Struct>]
type RSS =
    { Title: string
      LastUpdatedTime: DateTime
      Link: string }

[<Struct>]
type LoginForm = { Username: string; Password: string }

[<Struct>]
type LoginResult =
    { UserId: string
      RssUrls: string array
      SessionId: string }

[<Struct>]
type LoginError = { Message: string }

type LoginResponse =
    | Success of result: LoginResult
    | Failed of error: LoginError

type IRPCStore =
    { getRSSList: string array -> RSS seq Async
      loginOrRegister: LoginForm -> LoginResponse Async
      saveRSSUrls: (string * string array) -> unit Async
      initLogin: string -> LoginResponse Async }

module Route =
    let routeBuilder (typeName: string) (methodName: string) =
        sprintf "/rpc/%s/%s" typeName methodName
