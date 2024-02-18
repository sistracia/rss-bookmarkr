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
type LoginResponse = { UserId: string; RssUrls: string array }

type IRPCStore =
    { getRSSList: string array -> RSS seq Async
      loginOrRegister: LoginForm -> LoginResponse option Async
      saveRSSUrls: (string * string array) -> unit Async }

module Route =
    let routeBuilder (typeName: string) (methodName: string) =
        sprintf "/rpc/%s/%s" typeName methodName
