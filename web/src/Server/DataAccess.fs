module DataAccess

open System
open System.Threading
open Npgsql.FSharp

open Shared
open Types

let getUser (connectionString: string) (loginForm: LoginReq) : User option =
    connectionString
    |> Sql.connect
    |> Sql.query "SELECT id, username, password, COALESCE(email, '') as email FROM users WHERE username = @username"
    |> Sql.parameters [ "@username", Sql.string loginForm.Username ]
    |> Sql.execute (fun (read: RowReader) ->
        { User.Id = read.string "id"
          User.Username = read.text "username"
          User.Password = read.text "password"
          User.Email = read.text "email" })
    |> List.tryHead

let insertUser (connectionString: string) (loginForm: LoginReq) : string =
    let newUid: string = Guid.NewGuid().ToString()

    connectionString
    |> Sql.connect
    |> Sql.query "INSERT INTO users (id, username, password) VALUES (@id, @username, @password)"
    |> Sql.parameters
        [ "@id", Sql.text newUid
          "@username", Sql.text loginForm.Username
          "@password", Sql.text loginForm.Password ]
    |> Sql.executeNonQuery
    |> ignore

    newUid

let getRSSUrls (connectionString: string) (userId: string) : string list =
    connectionString
    |> Sql.connect
    |> Sql.query "SELECT url FROM rss_urls WHERE user_id = @user_id"
    |> Sql.parameters [ "@user_id", Sql.string userId ]
    |> Sql.execute (fun read -> read.text "url")

let insertUrls (connectionString: string) (userId: string) (urls: string array) =
    connectionString
    |> Sql.connect
    |> Sql.executeTransaction
        [ "INSERT INTO rss_urls (id, url, user_id) VALUES (@id, @url, @user_id)",
          [ yield!
                urls
                |> Array.map (fun (url: string) ->
                    [ "@id", Sql.text (Guid.NewGuid().ToString())
                      "@url", Sql.text url
                      "@user_id", Sql.text userId ]) ] ]
    |> ignore

let deleteUrls (connectionString: string) (userId: string) (urls: string array) : unit =
    connectionString
    |> Sql.connect
    |> Sql.query "DELETE FROM rss_urls WHERE user_id = @user_id AND url = ANY(@urls)"
    |> Sql.parameters [ "@user_id", Sql.text userId; "@urls", Sql.stringArray urls ]
    |> Sql.executeNonQuery
    |> ignore

let insertSession (connectionString: string) (userId: string) (sessionId: string) : unit =
    connectionString
    |> Sql.connect
    |> Sql.query "INSERT INTO sessions (id, user_id) VALUES (@id, @user_id)"
    |> Sql.parameters [ "@id", Sql.text sessionId; "@user_id", Sql.text userId ]
    |> Sql.executeNonQuery
    |> ignore

let getUserSession (connectionString: string) (sessionId: string) : User option =
    connectionString
    |> Sql.connect
    |> Sql.query
        """SELECT
                u.id AS user_id,
                u.username AS user_username,
                u.password AS user_password,
                COALESCE(u.email, '') as user_email
            FROM users u
            LEFT JOIN sessions s
                ON s.user_id = u.id
            WHERE s.id = @session_id"""
    |> Sql.parameters [ "@session_id", Sql.string sessionId ]
    |> Sql.execute (fun (read: RowReader) ->
        { User.Id = read.string "user_id"
          User.Username = read.text "user_username"
          User.Password = read.text "user_password"
          User.Email = read.text "user_email" })
    |> List.tryHead

let aggreateRssEmails (cancellationToken: CancellationToken) (connectionString: string) =
    connectionString
    |> Sql.connect
    |> Sql.cancellationToken cancellationToken
    |> Sql.query
        """SELECT
                u.id as user_id,
                COALESCE(u.email, '') as email,
                ARRAY_AGG(ru.url) AS urls
            FROM users u
            JOIN rss_urls ru
                ON u.id = ru.user_id
            WHERE u.email <> ''
            GROUP BY u.id"""
    |> Sql.execute (fun read ->
        { RSSEmailsAggregate.UserId = read.text "user_id"
          RSSEmailsAggregate.Email = read.text "email"
          RSSEmailsAggregate.Urls = read.stringArray "urls" })

let unsetUserEmail (connectionString: string) (email: string) : unit =
    connectionString
    |> Sql.connect
    |> Sql.query "UPDATE users SET email = NULL WHERE email = @email"
    |> Sql.parameters [ "@email", Sql.text email ]
    |> Sql.executeNonQuery
    |> ignore

let setUserEmail (connectionString: string) (userId: string, email: string) : unit =
    connectionString
    |> Sql.connect
    |> Sql.query "UPDATE users SET email = @email WHERE id = @id"
    |> Sql.parameters [ "@id", Sql.text userId; "@email", Sql.text email ]
    |> Sql.executeNonQuery
    |> ignore
