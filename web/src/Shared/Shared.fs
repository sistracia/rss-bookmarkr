namespace Shared

open System

[<Struct>]
type RSS =
    { Title: string
      LastUpdatedTime: DateTime
      Link: string }

[<Struct>]
type LoginForm = { Username: string; Password: string }

type IRPCStore =
    { getRSSList: string array -> RSS seq Async
      loginOrRegister: LoginForm -> string Async }

module Route =
    let routeBuilder (typeName: string) (methodName: string) =
        sprintf "/rpc/%s/%s" typeName methodName
