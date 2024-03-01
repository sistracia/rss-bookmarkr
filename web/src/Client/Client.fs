module App

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

    let getRSSList (urls: string array) = async { return! store.getRSSList urls }

    let loginOrRegister (loginForm: LoginForm) =
        async { return! store.loginOrRegister loginForm }

    let saveRSSUrlssss (userId: string, rssUrls: string array) =
        async { do! store.saveRSSUrls (userId, rssUrls) }

    let initLogin (sessionId: string) =
        async { return! store.initLogin sessionId }

module Search =
    type State = { Url: string; Urls: string array }

    type Msg =
        | SetUrl of string
        | AddUrl
        | SetUrls of string array
        | RemoveUrl of string

    let init () =
        { Urls = Array.empty; Url = "" }, Cmd.none

    let generateUrlSearch (urls: string seq) : string =
        match (urls |> String.concat ",") with
        | "" -> "/"
        | newUrl -> ("?url=" + newUrl)

    let update (msg: Msg) (state: State) : State * Cmd<Msg> =
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


    let render (state: State) (dispatch: Msg -> unit) : Fable.React.ReactElement =
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
                                                Html.span [ color.textNeutralContent; prop.text url ] ] ] ] ] ] ]

module Auth =
    type State =
        { InputUsername: string
          InputPassword: string
          LoggingIn: bool
          LoginError: string option
          User: User option }

    type Msg =
        | ChangeUsername of string
        | ChangePassword of string
        | Login
        | LoginSuccess of LoginResponse option
        | LoginFailed of error: string
        | Logout

    let init () =
        { InputUsername = ""
          InputPassword = ""
          LoggingIn = false
          LoginError = None
          User = None },
        Cmd.none

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

                nextState,
                Cmd.OfAsync.either RPC.loginOrRegister credentials LoginSuccess (fun ex -> LoginFailed ex.Message)
        | LoginSuccess(loginResponse: LoginResponse option) ->
            match loginResponse with
            | Some(response: LoginResponse) ->
                let user =
                    { User.SessionId = response.SessionId
                      User.UserId = response.UserId }

                Session.setSessionId response.SessionId
                let initState, _ = init ()
                let nextState = { initState with User = Some user }
                nextState, Cmd.none
            | None -> state, Cmd.none
        | LoginFailed(error: string) ->
            let nextState = { state with LoginError = Some error }
            nextState, Cmd.none
        | Logout -> state, Cmd.none

    let render (state: State) (dispatch: Msg -> unit) : Fable.React.ReactElement =
        React.fragment
            [ Daisy.navbar
                  [ prop.className "mb-2 shadow-lg bg-neutral text-neutral-content rounded-box"
                    prop.children
                        [ Daisy.navbarStart [ Html.h1 [ prop.text "RSS Bookmarkr" ] ]
                          Daisy.navbarEnd
                              [ match state.User with
                                | Some _ ->
                                    Daisy.button.label
                                        [ button.ghost
                                          prop.key "logout-button"
                                          prop.onClick (fun _ -> dispatch Logout)
                                          prop.text "Log Out" ]
                                | None ->
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
                                                        prop.onClick (fun _ -> dispatch Login) ] ] ] ] ] ] ] ]

type ServerState =
    | Idle
    | Loading
    | ServerError of string

type RSSState =
    { ServerState: ServerState
      RSSList: RSS seq }

type State =
    { Search: Search.State
      Auth: Auth.State
      RssState: RSSState }

type BrowserRoute = Search of string option

type Msg =
    | SearchMsg of Search.Msg
    | AuthMsg of Auth.Msg
    | SaveUrls
    | GotRSSList of RSS seq
    | RssErrorMsg of exn
    | InitUser

let route = oneOf [ map Search (top <?> stringParam "url") ]

let getUrlSearch (route: BrowserRoute option) =
    match route with
    | Some(Search(query)) ->
        match query with
        | Some query -> query.Split [| ',' |] |> Array.distinct
        | None -> [||]
    | _ -> [||]

let cmdGetRssList (urls: string array) =
    match urls with
    | [||] -> Cmd.none
    | _ -> Cmd.OfAsync.either RPC.getRSSList urls GotRSSList RssErrorMsg

let urlUpdate (route: BrowserRoute option) (state: State) =
    match route with
    | Some(Search(_)) ->
        let newUrls = (getUrlSearch route)

        let (newServerState, newRSSList) =
            match newUrls with
            | [||] -> (Idle, Seq.empty)
            | _ -> (Loading, state.RssState.RSSList)

        let rssState =
            { state.RssState with
                RSSList = newRSSList
                ServerState = newServerState }

        let searchState =
            { state.Search with
                Urls = newUrls
                Url = "" }

        { state with
            Search = searchState
            RssState = rssState },
        cmdGetRssList newUrls
    | None ->
        let rssState =
            { state.RssState with
                ServerState = Idle
                RSSList = Seq.empty }

        let searchState =
            { state.Search with
                Urls = Array.empty
                Url = "" }

        { state with
            Search = searchState
            RssState = rssState },
        Cmd.none

let init (route: BrowserRoute option) =
    let initUrls = getUrlSearch route

    let rssState =
        { RSSList = Seq.empty
          ServerState =
            match initUrls with
            | [||] -> Idle
            | _ -> Loading }

    let searchState, searchCmd = Search.init ()
    let authState, authCmd = Auth.init ()

    let initialModel =
        { Search = searchState
          Auth = authState
          RssState = rssState }

    let initialCmd =
        Cmd.batch
            [ Cmd.map SearchMsg searchCmd
              Cmd.map AuthMsg authCmd
              cmdGetRssList initUrls
              Cmd.ofMsg InitUser ]

    initialModel, initialCmd

let update (msg: Msg) (state: State) =
    match msg with
    | SearchMsg(searchMsg: Search.Msg) ->
        let nextSearchState, nextSearchCmd = Search.update searchMsg state.Search
        let nextState = { state with Search = nextSearchState }
        nextState, Cmd.map SearchMsg nextSearchCmd
    | AuthMsg(authMsg: Auth.Msg) ->
        match authMsg with
        | Auth.LoginSuccess(loginResponse: LoginResponse option) ->
            match loginResponse with
            | Some(loginResponse: LoginResponse) ->
                let nextAuthState, nextAuthCmd = Auth.update authMsg state.Auth
                let nextState = { state with Auth = nextAuthState }

                nextState,
                Cmd.batch
                    [ Cmd.map AuthMsg nextAuthCmd
                      Cmd.ofMsg (SearchMsg(Search.Msg.SetUrls loginResponse.RssUrls)) ]
            | None -> state, Cmd.none
        | Auth.Logout ->
            Session.removeSessionId ()
            let searchState, _ = Search.init ()
            let authState, _ = Auth.init ()

            let nextState =
                { State.Search = searchState
                  State.Auth = authState
                  RssState =
                    { RSSList = Seq.empty
                      ServerState = Idle } }

            nextState, Cmd.none
        | _ ->
            let nextAuthState, nextAuthCmd = Auth.update authMsg state.Auth
            let nextState = { state with Auth = nextAuthState }
            nextState, Cmd.map AuthMsg nextAuthCmd
    | InitUser ->
        match Session.tryGetSessionId () with
        | None -> state, Cmd.none
        | Some sessionId ->
            state, Cmd.OfAsync.either RPC.initLogin sessionId (Auth.Msg.LoginSuccess >> AuthMsg) RssErrorMsg
    | SaveUrls ->
        state,
        match state.Auth.User with
        | Some user -> Cmd.OfAsync.attempt RPC.saveRSSUrlssss (user.UserId, state.Search.Urls) RssErrorMsg
        | None -> Cmd.none
    | GotRSSList response ->
        { state with
            RssState =
                { state.RssState with
                    ServerState = Idle
                    RSSList = response } },
        Cmd.none
    | RssErrorMsg e ->
        { state with
            RssState =
                { state.RssState with
                    ServerState = ServerError e.Message } },
        Cmd.none

let render (state: State) (dispatch: Msg -> unit) : Fable.React.ReactElement =
    Html.div
        [ prop.className "max-w-[768px] p-5 mx-auto flex flex-col gap-3"
          prop.children
              [

                Auth.render state.Auth (AuthMsg >> dispatch)
                Search.render state.Search (SearchMsg >> dispatch)

                if state.Search.Urls.Length <> 0 && state.Auth.User <> None then
                    Html.div
                        [ prop.className "flex flex-wrap gap-3"
                          prop.children
                              [ Daisy.button.button
                                    [ button.link
                                      prop.onClick (fun _ -> dispatch SaveUrls)
                                      prop.text "Save Urls" ] ] ]
                Html.div
                    [ prop.className "flex flex-col gap-3"
                      prop.children
                          [ match state.RssState.ServerState with
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
                                    [ for rss in state.RssState.RSSList do
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
                            | ServerError errorMsg -> Daisy.alert [ alert.error; prop.text errorMsg ]

                            match state.Auth.LoginError with
                            | Some errorMsg -> Daisy.alert [ alert.error; prop.text errorMsg ]
                            | None -> () ] ] ] ]

Program.mkProgram init update render
|> Program.toNavigable (parsePath route) urlUpdate
|> Program.withReactSynchronous "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
