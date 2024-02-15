namespace Shared

open System

type RSS =
    struct
        val Title: string

        val LastUpdatedTime: DateTime

        val Link: string

        new(title: string, lastUpdatedTime: DateTime, link: string) =
            { Title = title
              LastUpdatedTime = lastUpdatedTime
              Link = link }
    end


type IRSSStore =
    { getRSSList: string array -> RSS seq Async }

module Route =
    let routeBuilder (typeName: string) (methodName: string) =
        sprintf "/rpc/%s/%s" typeName methodName
