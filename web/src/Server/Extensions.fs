module Extensions

open Microsoft.Extensions.Configuration
open Microsoft.AspNetCore.Http
open Giraffe

let rssDbConnectionStringKey = "RssDb"

/// Ref: https://github.com/CompositionalIT/TodoService/blob/main/src/app/Todo.Api.fs
type HttpContext with

    /// The SQL connection string to the RSS database.
    member this.RssDbConnectionString =
        match this.GetService<IConfiguration>().GetConnectionString rssDbConnectionStringKey with
        | null -> failwith "Missing connection string"
        | v -> v
