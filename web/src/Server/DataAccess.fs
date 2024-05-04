module DataAccess

open System
open System.Threading
open Npgsql.FSharp

open Shared
open Types

let getUser (connectionString: string) (loginForm: LoginForm) : User option =
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

let insertUser (connectionString: string) (loginForm: LoginForm) : string =
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
                    COALESCE(u.email, '') as email,
                    ARRAY_AGG(ru.url || '|' || rh.latest_updated) AS history_pairs
                FROM
                    users u
                JOIN
                    rss_urls ru ON u.id = ru.user_id 
                JOIN
                    rss_histories rh ON ru .url = rh.url
                GROUP BY
                    u.email;"""
    |> Sql.execute (fun read ->
        { RSSEmailsAggregate.Email = read.text "email"
          RSSEmailsAggregate.HistoryPairs =
            (read.stringArray "history_pairs")
            |> Array.map (fun (pair: string) ->
                match pair.Split("|") with
                | [| url: string; latestUpdated: string |] ->
                    Some
                        { RSSHistory.Url = url
                          RSSHistory.LatestUpdated = DateTime.Parse latestUpdated }
                | _ -> None) })

let unsetUserEmail (connectionString: string) (email: string) : unit =
    connectionString
    |> Sql.connect
    |> Sql.query "UPDATE users SET email = NULL WHERE email = @email"
    |> Sql.parameters [ "@email", Sql.text email ]
    |> Sql.executeNonQuery
    |> ignore

let setUserEmail (connectionString: string) (userId: string, email: string) (newRSSHistories: RSSHistory seq) : unit =
    connectionString
    |> Sql.connect
    |> Sql.executeTransaction
        [ "UPDATE users SET email = @email WHERE id = @id", [ [ "@id", Sql.text userId; "@email", Sql.text email ] ]
          """INSERT INTO rss_histories (id, url, latest_updated) 
                            VALUES (@id, @url, @latest_updated)
                            ON CONFLICT (url) DO NOTHING""",
          [ yield!
                newRSSHistories
                |> Seq.map (fun (rss: RSSHistory) ->
                    [ "@id", Sql.text (Guid.NewGuid().ToString())
                      "@url", Sql.text rss.Url
                      "@latest_updated", Sql.date rss.LatestUpdated ]) ] ]
    |> ignore

// Upsert the RSS histories
let renewRSSHistories
    (cancellationToken: CancellationToken)
    (connectionString: string)
    (newRSSHistories: RSSHistory seq)
    : unit =
    connectionString
    |> Sql.connect
    |> Sql.cancellationToken cancellationToken
    |> Sql.executeTransaction
        [ """INSERT INTO rss_histories (id, url, latest_updated) 
                            VALUES (@id, @url, @latest_updated)
                            ON CONFLICT (url)
                                DO UPDATE 
                                    SET latest_updated=EXCLUDED.latest_updated""",
          [ yield!
                newRSSHistories
                |> Seq.map (fun (rss: RSSHistory) ->
                    [ "@id", Sql.text (Guid.NewGuid().ToString())
                      "@url", Sql.text rss.Url
                      "@latest_updated", Sql.date rss.LatestUpdated ]) ] ]
    |> ignore
