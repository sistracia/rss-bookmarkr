namespace Shared

open System

[<Struct>]
type RSS =
    { Title: string
      LastUpdatedTime: DateTime
      Link: string }


type IRSSStore =
    { getRSSList: string array -> RSS seq Async }

module Route =
    let routeBuilder (typeName: string) (methodName: string) =
        sprintf "/rpc/%s/%s" typeName methodName
