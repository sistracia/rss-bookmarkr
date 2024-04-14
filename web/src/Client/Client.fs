module App

open System
open Feliz
open Elmish
open Elmish.React
open Elmish.Navigation
open Elmish.UrlParser
open Fable.Remoting.Client
open Feliz.DaisyUI
open Feliz.DaisyUI.Operators
open Shared

#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

type User =
    { UserId: string
      SessionId: string
      Email: string option }


/// Copy from https://github.com/Dzoukr/Yobo/blob/master/src/Yobo.Client/TokenStorage.fs
module Session =
    let sessionKey = "session_id"

    let tryGetSessionId () : string option =
        Browser.WebStorage.localStorage.getItem (sessionKey)
        |> (function
        | null -> None
        | (x: string) -> Some(x))
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

    let subscribe (userId: string, email: string) : unit Async =
        async { do! store.subscribe (userId, email) }

    let unsubscribe (email: string) : unit Async = async { do! store.unsubscribe email }

module Component =
    let renderError (error: string option) : Fable.React.ReactElement =
        match error with
        | Some(error: string) ->
            Daisy.toast
                [ prop.style [ style.zIndex 1 ]
                  prop.children [ Daisy.alert [ alert.error; prop.text error ] ] ]
        | None -> React.fragment []

module Search =
    let generateUrlSearch (urls: string seq) : string =
        match (urls |> String.concat ",") with
        | "" -> "/"
        | newUrl -> ("?url=" + newUrl)


module RSS =

    type ServerState =
        | Idle
        | Loading
        | Error of exn option

    type State =
        { Url: string
          Urls: string array
          Error: string option
          ServerState: ServerState
          RSSList: RSS seq
          Email: string }

    type Msg =
        | SetUrl of string
        | AddUrl
        | SetUrls of string array
        | RemoveUrl of string
        | SaveUrls
        | SetError of error: string option
        | GetRSSList of urls: string array
        | GotRSSList of RSS seq
        | ChangeEmail of string
        | Subscribe of userId: string
        | OnSubscribed of unit
        | Unsubscribe of email: string
        | OnUnsubscribe of unit
        | SubscriptionChange of email: string

    let init () =
        let initialState =
            { State.Urls = Array.empty
              State.Url = ""
              State.Error = None
              State.RSSList = Seq.empty
              State.ServerState = Idle
              State.Email = "" }

        initialState, Cmd.none

    let update (user: User option) (msg: Msg) (state: State) : State * Cmd<Msg> =
        match msg with
        | SetUrl(url: string) ->
            let nextState = { state with Url = url }
            nextState, Cmd.none
        | AddUrl ->
            let url = state.Url
            let isUrlExists = state.Urls |> Array.exists (fun elm -> elm = url)

            if url <> "" && not isUrlExists then
                let newUrls = [| url |]
                let newUrlsState = Array.append state.Urls newUrls

                let nextState =
                    { state with
                        Url = ""
                        Urls = newUrlsState }

                let nextCmd =
                    Cmd.batch
                        [ newUrlsState |> Search.generateUrlSearch |> Navigation.newUrl
                          newUrls |> GetRSSList |> Cmd.ofMsg ]

                nextState, nextCmd
            else
                let nextState = { state with Url = "" }
                nextState, Cmd.none
        | SetUrls(urls: string array) ->
            let nextState =
                { state with
                    Url = ""
                    Urls = urls
                    RSSList = Seq.empty }

            let nextCmd =
                match urls with
                | [||] -> Cmd.none
                | _ ->
                    Cmd.batch
                        [ urls |> Search.generateUrlSearch |> Navigation.newUrl
                          urls |> GetRSSList |> Cmd.ofMsg ]

            nextState, nextCmd
        | RemoveUrl(url: string) ->
            let newUrls = state.Urls |> Array.filter (fun elm -> elm <> url)

            let newRSSList =
                state.RSSList
                |> Seq.filter (fun (rss: RSS) -> newUrls |> Array.exists (fun (newUrl: string) -> newUrl = rss.Origin))

            let nextState =
                { state with
                    Urls = newUrls
                    RSSList = newRSSList }

            nextState, newUrls |> Search.generateUrlSearch |> Navigation.newUrl
        | SaveUrls ->
            state,
            match user with
            | Some(user: User) ->
                let ofError = fun (ex: exn) -> Some ex.Message |> SetError
                Cmd.OfAsync.attempt RPC.saveRSSUrlssss (user.UserId, state.Urls) ofError
            | None -> Cmd.none
        | GetRSSList(urls: string array) ->
            let nextState = { state with ServerState = Loading }
            let ofError = fun (ex: exn) -> Some ex.Message |> SetError
            let nextCmd = Cmd.OfAsync.either RPC.getRSSList urls GotRSSList ofError
            nextState, nextCmd
        | GotRSSList(rssList: RSS seq) ->
            { state with
                ServerState = Idle
                RSSList = [ rssList; state.RSSList ] |> Seq.concat |> Seq.sortByDescending _.PublishDate },
            Cmd.none
        | ChangeEmail(email: string) ->
            let nextState = { state with Email = email }
            nextState, Cmd.none
        | Subscribe(userId: string) ->
            let ofError = fun (ex: exn) -> Some ex.Message |> SetError
            state, Cmd.OfAsync.either RPC.subscribe (userId, state.Email) OnSubscribed ofError
        | OnSubscribed() ->
            let nextState = { state with Email = "" }
            nextState, SubscriptionChange state.Email |> Cmd.ofMsg
        | Unsubscribe(email: string) ->
            let ofError = fun (ex: exn) -> Some ex.Message |> SetError
            state, Cmd.OfAsync.either RPC.unsubscribe email OnUnsubscribe ofError
        | OnUnsubscribe() -> state, SubscriptionChange "" |> Cmd.ofMsg
        | SubscriptionChange(_: string) -> state, Cmd.none
        | SetError(error: string option) ->
            let nextState = { state with Error = error }
            nextState, Cmd.none

    let render (user: User option) (state: State) (dispatch: Msg -> unit) : Fable.React.ReactElement =
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
                                              Daisy.skeleton [ prop.className "h-4 w-1/4" ]
                                              Daisy.skeleton [ prop.className "h-4 w-1/2" ]
                                              Daisy.skeleton [ prop.className "h-4 w-1/4" ] ] ] ]
                    | Idle ->
                        Html.div
                            [ prop.className "w-full flex flex-wrap gap-3"
                              prop.children
                                  [ Daisy.input
                                        [ input.bordered
                                          prop.value state.Url
                                          prop.onChange (SetUrl >> dispatch)
                                          prop.className "flex-1"
                                          prop.placeholder "https://overreacted.io/rss.xml" ]
                                    Daisy.button.button
                                        [ button.neutral; prop.onClick (fun _ -> dispatch AddUrl); prop.text "Add" ] ] ]

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

                        if user <> None then
                            Html.div
                                [ prop.className "flex flex-wrap gap-3"
                                  prop.children
                                      [ Daisy.button.button
                                            [ button.link
                                              prop.onClick (fun _ -> dispatch SaveUrls)
                                              prop.text "Save Urls" ]
                                        match user with
                                        | None -> ()
                                        | Some(user: User) ->
                                            match user.Email with
                                            | Some(email: string) ->
                                                Daisy.button.button
                                                    [ button.link
                                                      prop.onClick (fun _ -> dispatch (Unsubscribe email))
                                                      prop.text "Unsubscribe" ]
                                            | None ->
                                                React.fragment
                                                    [ Daisy.button.label
                                                          [ button.link
                                                            prop.key "subscribe-button"
                                                            prop.htmlFor "subscribe-modal"
                                                            prop.text "Subscribe" ]

                                                      Html.div
                                                          [ Daisy.modalToggle [ prop.id "subscribe-modal" ]
                                                            Daisy.modal.div
                                                                [ prop.children
                                                                      [ Daisy.modalBox.div
                                                                            [ Html.form
                                                                                  [ Html.h2
                                                                                        [ prop.text "Subscribe Form" ]
                                                                                    Daisy.formControl
                                                                                        [ Daisy.label
                                                                                              [ prop.htmlFor
                                                                                                    "subscribe-email-field"
                                                                                                prop.children
                                                                                                    [ Daisy.labelText
                                                                                                          "E-Mail" ] ]
                                                                                          Daisy.input
                                                                                              [ input.bordered
                                                                                                prop.id
                                                                                                    "subscribe-email-field"
                                                                                                prop.placeholder
                                                                                                    "email@domain.com"
                                                                                                prop.required true
                                                                                                prop.value state.Email
                                                                                                prop.onChange (
                                                                                                    ChangeEmail
                                                                                                    >> dispatch
                                                                                                ) ] ]
                                                                                    Daisy.modalAction
                                                                                        [ Daisy.button.label
                                                                                              [ prop.htmlFor
                                                                                                    "subscribe-modal"
                                                                                                prop.text "Cancel" ]
                                                                                          Daisy.button.label
                                                                                              [ button.neutral
                                                                                                prop.htmlFor
                                                                                                    "subscribe-modal"
                                                                                                prop.text "Subscribe"
                                                                                                prop.type' "submit"
                                                                                                prop.onClick (fun _ ->
                                                                                                    dispatch (
                                                                                                        Subscribe
                                                                                                            user.UserId
                                                                                                    )) ] ] ] ] ] ] ] ] ] ]

                        Component.renderError state.Error

                        yield!
                            [ for rss in state.RSSList do
                                  Daisy.card
                                      [ card.bordered
                                        prop.children
                                            [ Daisy.cardBody
                                                  [ Daisy.cardTitle rss.Title
                                                    Daisy.link
                                                        [ prop.href rss.OriginHostUrl
                                                          prop.target "_blank"
                                                          prop.rel "noopener"
                                                          prop.text rss.OriginHost ]
                                                    Html.p (sprintf $"{rss.PublishDate.ToString()} ({rss.TimeAgo})")
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

    let update (msg: Msg) (state: State) : State * Cmd<Msg> =
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
        | LoginSuccess(_: LoginResult option)
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


type State =
    { User: User option
      RSS: RSS.State
      Auth: Auth.State }

type Msg =
    | RSSMsg of RSS.Msg
    | AuthMsg of Auth.Msg

type BrowserRoute = Search of string option

let initUrlCmd (urls: string array) =
    urls |> RSS.Msg.SetUrls |> RSSMsg |> Cmd.ofMsg

let route = oneOf [ map Search (top <?> stringParam "url") ]

let urlUpdate (_: BrowserRoute option) (state: State) = state, Cmd.none

let urlInit (route: BrowserRoute option) (state: State) =
    state,
    match route with
    | Some(Search(query: string option)) ->
        match query with
        | Some query -> query.Split [| ',' |] |> Array.distinct
        | None -> [||]
    | None -> [||]
    |> initUrlCmd


let init (route: BrowserRoute option) =
    let rssState, rssCmd = RSS.init ()
    let authState, authCmd = Auth.init ()

    let initialState =
        { User = None
          RSS = rssState
          Auth = authState }

    let _initialState, initalCmd = urlInit route initialState

    let _initialCmd =
        Cmd.batch [ Cmd.map RSSMsg rssCmd; Cmd.map AuthMsg authCmd; initalCmd ]

    _initialState, _initialCmd

let update (msg: Msg) (state: State) : State * Cmd<Msg> =
    match msg with
    | RSSMsg(rssMsg: RSS.Msg) ->
        match rssMsg with
        | RSS.SubscriptionChange(email: string) ->
            let user =
                match state.User with
                | None -> state.User
                | Some(user: User) ->
                    let newUser: User = { user with Email = Some email }
                    Some newUser

            let nextState = { state with User = user }
            nextState, Cmd.none
        | _ ->
            let nextRSSState, nextRSSCmd = RSS.update state.User rssMsg state.RSS
            let nextState = { state with RSS = nextRSSState }
            nextState, Cmd.map RSSMsg nextRSSCmd
    | AuthMsg(authMsg: Auth.Msg) ->
        match authMsg with
        | Auth.LoginSuccess(loginResult: LoginResult option) ->
            match loginResult with
            | Some(loginResult: LoginResult) ->
                Session.setSessionId loginResult.SessionId

                let authState, _ = Auth.init ()

                let user =
                    { User.SessionId = loginResult.SessionId
                      User.UserId = loginResult.UserId
                      User.Email = loginResult.Email }

                let nextState =
                    { state with
                        Auth = authState
                        State.User = Some user }

                let nextCmd = loginResult.RssUrls |> initUrlCmd

                nextState, nextCmd
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

let render (state: State) (dispatch: Msg -> unit) : Fable.React.ReactElement =
    let isLoggedIn = state.User <> None

    Html.div
        [ prop.className "max-w-[768px] p-5 mx-auto flex flex-col gap-3"
          prop.children
              [ Auth.render isLoggedIn state.Auth (AuthMsg >> dispatch)
                RSS.render state.User state.RSS (RSSMsg >> dispatch) ] ]

Program.mkProgram init update render
|> Program.toNavigable (parsePath route) urlUpdate
|> Program.withReactBatched "elmish-app"
#if DEBUG
|> Program.withConsoleTrace
|> Program.withDebugger
#endif
|> Program.run
