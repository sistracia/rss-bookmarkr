namespace Shared

open System

[<Struct>]
type RSS =
    { Title: string
      LastUpdatedTime: DateTime
      TimeAgo: string
      Link: string }

[<Struct>]
type LoginForm = { Username: string; Password: string }

[<Struct>]
type LoginResult =
    { UserId: string
      RssUrls: string array
      SessionId: string
      IsSubscribing: bool }

[<Struct>]
type LoginError = { Message: string }

type LoginResponse =
    | Success of result: LoginResult
    | Failed of error: LoginError

type IRPCStore =
    { getRSSList: string array -> RSS seq Async
      loginOrRegister: LoginForm -> LoginResponse Async
      saveRSSUrls: (string * string array) -> unit Async
      initLogin: string -> LoginResponse Async
      subscribe: (string * string) -> unit Async
      unsubscribe: string -> unit Async }

module Route =
    let routeBuilder (typeName: string) (methodName: string) =
        sprintf "/rpc/%s/%s" typeName methodName
