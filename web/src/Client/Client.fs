﻿module App

open System
open Feliz
open Elmish
open Elmish.Navigation
open Elmish.UrlParser
open Elmish.React
open Fable.Remoting.Client
open Feliz.DaisyUI
open Feliz.DaisyUI.Operators
open Shared

#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

type User = { UserId: string; SessionId: string }

/// Copy from https://github.com/Dzoukr/Yobo/blob/master/src/Yobo.Client/TokenStorage.fs
module Session =
    let sessionKey = "session_id"

    let tryGetSessionId () : string option =
        Browser.WebStorage.localStorage.getItem (sessionKey)
        |> (function
        | null -> None
        | x -> Some(x))
        |> Option.bind (fun x -> if String.IsNullOrWhiteSpace(x) then None else Some x)

    let removeSessionId () =
        Browser.WebStorage.localStorage.removeItem (sessionKey)

    let setSessionId (sessionId: string) =
        if String.IsNullOrWhiteSpace(sessionId) then
            removeSessionId ()
        else
            Browser.WebStorage.localStorage.setItem (sessionKey, sessionId)

module RPC =
    let store =
        Remoting.createApi ()
        |> Remoting.withRouteBuilder Route.routeBuilder
        |> Remoting.buildProxy<IRPCStore>

    let getRSSList (urls: string array) : Async<RSS seq> = async { return! store.getRSSList urls }

    let loginOrRegister (loginForm: LoginForm) : LoginResponse Async =
        async { return! store.loginOrRegister loginForm }

    let saveRSSUrlssss (userId: string, rssUrls: string array) : unit Async =
        async { do! store.saveRSSUrls (userId, rssUrls) }

    let initLogin (sessionId: string) : LoginResponse Async =
        async { return! store.initLogin sessionId }

module Component =
    let renderError (error: string option) : Fable.React.ReactElement =
        match error with
        | Some(error: string) ->
            Daisy.toast
                [ prop.style [ style.zIndex 1 ]
                  prop.children [ Daisy.alert [ alert.error; prop.text error ] ] ]
        | None -> React.fragment []

module Search =

    type State =
        { Url: string
          Urls: string array
          Error: string option }

    type Msg =
        | SetUrl of string
        | AddUrl
        | SetUrls of string array
        | RemoveUrl of string
        | SaveUrls
        | SetError of error: string option

    let init () =
        { State.Urls = Array.empty
          State.Url = ""
          Error = None },
        Cmd.none

    let generateUrlSearch (urls: string seq) : string =
        match (urls |> String.concat ",") with
        | "" -> "/"
        | newUrl -> ("?url=" + newUrl)

    let update (user: User option) (msg: Msg) (state: State) : State * Cmd<Msg> =
        match msg with
        | SetUrl(url: string) ->
            let nextState = { state with Url = url }
            nextState, Cmd.none
        | AddUrl ->
            let url = state.Url
            let isUrlExists = state.Urls |> Array.exists (fun elm -> elm = url)

            let nextState, nextCmd =
                if url <> "" && not isUrlExists then
                    state, Array.append state.Urls [| url |] |> generateUrlSearch |> Navigation.newUrl
                else
                    { state with Url = "" }, Cmd.none

            nextState, nextCmd
        | SetUrls(urls: string array) ->
            let nextState = { state with Url = ""; Urls = urls }
            nextState, Navigation.newUrl (generateUrlSearch urls)
        | RemoveUrl(url: string) ->
            state,
            state.Urls
            |> Array.filter (fun elm -> elm <> url)
            |> generateUrlSearch
            |> Navigation.newUrl
        | SaveUrls ->
            state,
            match user with
            | Some(user: User) ->
                let ofError = fun (ex: exn) -> Some ex.Message |> SetError
                Cmd.OfAsync.attempt RPC.saveRSSUrlssss (user.UserId, state.Urls) ofError
            | None -> Cmd.none
        | SetError(error: string option) ->
            let nextState = { state with Error = error }
            nextState, Cmd.none

    let render (isLoggedIn: bool) (state: State) (dispatch: Msg -> unit) : Fable.React.ReactElement =
        React.fragment
            [ Html.div
                  [ prop.className "w-full flex flex-wrap gap-3"
                    prop.children[Daisy.input
                                      [ input.bordered
                                        prop.value state.Url
                                        prop.onChange (SetUrl >> dispatch)
                                        prop.className "flex-1"
                                        prop.placeholder "https://overreacted.io/rss.xml" ]

                                  Daisy.button.button
                                      [ button.neutral; prop.onClick (fun _ -> dispatch AddUrl); prop.text "Add" ]] ]
              Html.div
                  [ prop.className "flex flex-wrap gap-3"
                    prop.children
                        [ yield!
                              [ for url in state.Urls do
                                    Html.div
                                        [ (color.bgNeutral ++ (prop.className "p-1 rounded-lg flex gap-1"))
                                          prop.children
                                              [ Daisy.button.button
                                                    [ button.error
                                                      button.xs
                                                      prop.onClick (fun _ -> dispatch (RemoveUrl url))
                                                      prop.text "X" ]
                                                Html.span [ color.textNeutralContent; prop.text url ] ] ] ] ] ]
              if state.Urls.Length <> 0 && isLoggedIn then
                  Html.div
                      [ prop.className "flex flex-wrap gap-3"
                        prop.children
                            [ Daisy.button.button
                                  [ button.link
                                    prop.onClick (fun _ -> dispatch SaveUrls)
                                    prop.text "Save Urls" ] ] ]
              Component.renderError state.Error ]

module Auth =

    type State =
        { InputUsername: string
          InputPassword: string
          LoggingIn: bool
          Error: string option }

    type Msg =
        | ChangeUsername of string
        | ChangePassword of string
        | Login
        | InitUser
        | LoginSuccess of LoginResult option
        | SetError of error: string option
        | Logout

    let init () =
        { State.InputUsername = ""
          State.InputPassword = ""
          State.LoggingIn = false
          Error = None },
        InitUser |> Cmd.ofMsg

    let update (msg: Msg) (state: State) =
        match msg with
        | ChangeUsername(username: string) ->
            let nextState = { state with InputUsername = username }
            nextState, Cmd.none
        | ChangePassword(password: string) ->
            let nextState = { state with InputPassword = password }
            nextState, Cmd.none
        | Login ->
            let isFormFilled = state.InputUsername <> "" && state.InputPassword <> ""

            if not isFormFilled then
                state, Cmd.none
            else
                let nextState = { state with LoggingIn = true }

                let credentials =
                    { LoginForm.Username = state.InputUsername
                      LoginForm.Password = state.InputPassword }

                let ofSuccess =
                    function
                    | Success(loginResult: LoginResult) -> Some loginResult |> LoginSuccess
                    | Failed(_: LoginError) -> Some "The password you entered is incorrect." |> SetError

                let ofError = (fun (ex: exn) -> Some ex.Message |> SetError)

                nextState, Cmd.OfAsync.either RPC.loginOrRegister credentials ofSuccess ofError
        | InitUser ->
            match Session.tryGetSessionId () with
            | None -> state, Cmd.none
            | Some sessionId ->
                let ofSuccess =
                    function
                    | Success(loginResult: LoginResult) -> Some loginResult |> LoginSuccess
                    | _ -> Some "Failed to login account." |> SetError

                let ofError = (fun (ex: exn) -> Some ex.Message |> SetError)
                state, Cmd.OfAsync.either RPC.initLogin sessionId ofSuccess ofError
        | SetError(error: string option) ->
            let nextState =
                { state with
                    LoggingIn = false
                    Error = error }

            nextState, Cmd.none
        | LoginSuccess(_: LoginResult option) -> state, Cmd.none
        | Logout -> state, Cmd.none

    let render (isLoggedIn: bool) (state: State) (dispatch: Msg -> unit) : Fable.React.ReactElement =
        React.fragment
            [ Daisy.navbar
                  [ prop.className "mb-2 shadow-lg bg-neutral text-neutral-content rounded-box"
                    prop.children
                        [ Daisy.navbarStart [ Html.h1 [ prop.text "RSS Bookmarkr" ] ]
                          Daisy.navbarEnd
                              [ match isLoggedIn with
                                | true ->
                                    Daisy.button.label
                                        [ button.ghost
                                          prop.key "logout-button"
                                          prop.onClick (fun _ -> dispatch Logout)
                                          prop.text "Log Out" ]
                                | false ->
                                    Daisy.button.label
                                        [ button.ghost
                                          prop.key "login-button"
                                          prop.htmlFor "login-modal"
                                          prop.text "Log In" ] ] ] ]
              Html.div
                  [ Daisy.modalToggle [ prop.id "login-modal" ]
                    Daisy.modal.div
                        [ prop.children
                              [ Daisy.modalBox.div
                                    [ Html.form
                                          [ Html.h2 [ prop.text "Log In Form" ]
                                            Daisy.formControl
                                                [ Daisy.label
                                                      [ prop.htmlFor "login-username-field"
                                                        prop.children [ Daisy.labelText "Username" ] ]
                                                  Daisy.input
                                                      [ input.bordered
                                                        prop.id "login-username-field"
                                                        prop.placeholder "Username"
                                                        prop.required true
                                                        prop.value state.InputUsername
                                                        prop.onChange (ChangeUsername >> dispatch) ] ]
                                            Daisy.formControl
                                                [ Daisy.label
                                                      [ prop.htmlFor "password-username-field"
                                                        prop.children [ Daisy.labelText "Password" ] ]
                                                  Daisy.input
                                                      [ input.bordered
                                                        prop.id "password-username-field"
                                                        prop.type' "password"
                                                        prop.placeholder "******"
                                                        prop.required true
                                                        prop.value state.InputPassword
                                                        prop.onChange (ChangePassword >> dispatch) ] ]
                                            Html.p "Account will be automatically created if not exist."
                                            Daisy.modalAction
                                                [ Daisy.button.label [ prop.htmlFor "login-modal"; prop.text "Cancel" ]
                                                  Daisy.button.label
                                                      [ button.neutral
                                                        prop.htmlFor "login-modal"
                                                        prop.text "Log In"
                                                        prop.type' "submit"
                                                        prop.onClick (fun _ -> dispatch Login) ] ] ] ] ] ] ]
              Component.renderError state.Error ]

module RSS =

    type ServerState =
        | Idle
        | Loading
        | Error of exn option

    type State =
        { ServerState: ServerState
          RSSList: RSS seq }

    type Msg =
        | GetRSSList of urls: string array
        | GotRSSList of RSS seq
        | SetError of error: string option

    let init () =
        { State.RSSList = Seq.empty
          State.ServerState = Idle },
        Cmd.none

    let update (msg: Msg) (state: State) =
        match msg with
        | GetRSSList(urls: string array) ->
            match urls with
            | [||] ->
                let nextState =
                    { state with
                        ServerState = Idle
                        RSSList = Seq.empty }

                (nextState, Cmd.none)
            | _ ->
                let nextState = { state with ServerState = Loading }
                let ofError = fun (ex: exn) -> Some ex.Message |> SetError
                nextState, Cmd.OfAsync.either RPC.getRSSList urls GotRSSList ofError
        | GotRSSList(rssList: RSS seq) ->
            { state with
                ServerState = Idle
                RSSList = rssList },
            Cmd.none
        | SetError(_: string option) -> state, Cmd.none

    let view (state: State) : Fable.React.ReactElement =
        Html.div
            [ prop.className "flex flex-col gap-3"
              prop.children
                  [ match state.ServerState with
                    | Loading ->
                        yield!
                            [ for _ in 0..5 do
                                  Daisy.card
                                      [ card.bordered
                                        prop.className "flex flex-col gap-3 p-8"
                                        prop.children
                                            [ Daisy.skeleton [ prop.className "h-6 w-full" ]
                                              Daisy.skeleton [ prop.className "h-4 w-1/2" ]
                                              Daisy.skeleton [ prop.className "h-4 w-1/4" ] ] ] ]
                    | Idle ->
                        yield!
                            [ for rss in state.RSSList do
                                  Daisy.card
                                      [ card.bordered
                                        prop.children
                                            [ Daisy.cardBody
                                                  [ Daisy.cardTitle rss.Title
                                                    Html.p (rss.LastUpdatedTime.ToString())
                                                    Daisy.cardActions
                                                        [ Daisy.link
                                                              [ prop.href rss.Link
                                                                prop.target "_blank"
                                                                prop.rel "noopener"
                                                                prop.text "Read" ] ] ] ] ] ]
                    | Error(error: exn option) ->
                        match error with
                        | Some(error: exn) -> Component.renderError (Some error.Message)
                        | None -> () ] ]

type State =
    { User: User option
      Search: Search.State
      Auth: Auth.State
      RSS: RSS.State }

type Msg =
    | SearchMsg of Search.Msg
    | AuthMsg of Auth.Msg
    | RSSMsg of RSS.Msg
    | InitRSS of urls: string array

type BrowserRoute = Search of string option

let route = oneOf [ map Search (top <?> stringParam "url") ]

let getUrlSearch (route: BrowserRoute option) =
    match route with
    | Some(Search(query: string option)) ->
        match query with
        | Some query -> query.Split [| ',' |] |> Array.distinct
        | None -> [||]
    | _ -> [||]

let urlUpdate (route: BrowserRoute option) (state: State) =
    match route with
    | Some(Search(_: string option)) -> state, route |> (getUrlSearch) |> InitRSS |> Cmd.ofMsg
    | None -> state, [||] |> InitRSS |> Cmd.ofMsg

let init (_: BrowserRoute option) =
    let rssState, rssCmd = RSS.init ()
    let searchState, searchCmd = Search.init ()
    let authState, authCmd = Auth.init ()

    let initialModel =
        { User = None
          Search = searchState
          Auth = authState
          RSS = rssState }

    let initialCmd =
        Cmd.batch [ Cmd.map SearchMsg searchCmd; Cmd.map AuthMsg authCmd; Cmd.map RSSMsg rssCmd ]

    initialModel, initialCmd

let update (msg: Msg) (state: State) =
    match msg with
    | SearchMsg(searchMsg: Search.Msg) ->
        let nextSearchState, nextSearchCmd = Search.update state.User searchMsg state.Search
        let nextState = { state with Search = nextSearchState }
        nextState, Cmd.map SearchMsg nextSearchCmd
    | AuthMsg(authMsg: Auth.Msg) ->
        match authMsg with
        | Auth.LoginSuccess(loginResult: LoginResult option) ->
            match loginResult with
            | Some(loginResult: LoginResult) ->
                Session.setSessionId loginResult.SessionId

                let authState, _ = Auth.init ()

                let user =
                    { User.SessionId = loginResult.SessionId
                      User.UserId = loginResult.UserId }

                let nextState =
                    { state with
                        Auth = authState
                        State.User = Some user }

                nextState, loginResult.RssUrls |> Search.Msg.SetUrls |> SearchMsg |> Cmd.ofMsg
            | None -> state, Cmd.none
        | Auth.Logout ->
            Session.removeSessionId ()
            let authState, _ = Auth.init ()

            let nextState =
                { state with
                    State.Auth = authState
                    State.User = None }

            nextState, Cmd.none
        | _ ->
            let nextAuthState, nextAuthCmd = Auth.update authMsg state.Auth
            let nextState = { state with Auth = nextAuthState }
            nextState, Cmd.map AuthMsg nextAuthCmd
    | RSSMsg(rssMsg: RSS.Msg) ->
        let nextRSSState, nextRSSCmd = RSS.update rssMsg state.RSS
        let nextState = { state with RSS = nextRSSState }
        nextState, Cmd.map RSSMsg nextRSSCmd
    | InitRSS(urls: string array) ->
        let nextCmd =
            Cmd.batch
                [ urls |> Search.Msg.SetUrls |> SearchMsg |> Cmd.ofMsg
                  urls |> RSS.Msg.GetRSSList |> RSSMsg |> Cmd.ofMsg ]

        state, nextCmd

let render (state: State) (dispatch: Msg -> unit) : Fable.React.ReactElement =
    let isLoggedIn = state.User <> None

    Html.div
        [ prop.className "max-w-[768px] p-5 mx-auto flex flex-col gap-3"
          prop.children
              [ Auth.render isLoggedIn state.Auth (AuthMsg >> dispatch)
                Search.render isLoggedIn state.Search (SearchMsg >> dispatch)
                RSS.view state.RSS ] ]

Program.mkProgram init update render
|> Program.toNavigable (parsePath route) urlUpdate
|> Program.withReactBatched "elmish-app"
#if DEBUG
|> Program.withConsoleTrace
|> Program.withDebugger
#endif
|> Program.run
