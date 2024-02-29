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

module Search =
    type State = { Url: string; Urls: string array }

    type Msg =
        | SetUrl of string
        | AddUrl
        | SetUrls of string array
        | RemoveUrl of string

    let init () =
        { Urls = Array.empty; Url = "" }, Cmd.none

    let generateUrlSearch (urls: string seq) =
        match (urls |> String.concat ",") with
        | "" -> "/"
        | newUrl -> ("?url=" + newUrl)

    let update (msg: Msg) (state: State) : State * Cmd<Msg> =
        match msg with
        | SetUrl(url: string) -> { state with Url = url }, Cmd.none
        | AddUrl ->
            let url = state.Url
            let isUrlExists = state.Urls |> Array.exists (fun elm -> elm = url)

            if url <> "" && not isUrlExists then
                state, Array.append state.Urls [| url |] |> generateUrlSearch |> Navigation.newUrl
            else
                { state with Url = "" }, Cmd.none
        | SetUrls(urls: string array) ->
            { state with Url = ""; Urls = urls }, Navigation.newUrl (generateUrlSearch urls)
        | RemoveUrl(url: string) ->
            state,
            state.Urls
            |> Array.filter (fun elm -> elm <> url)
            |> generateUrlSearch
            |> Navigation.newUrl


    let render (state: State) (dispatch: Msg -> unit) =
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

type ServerState =
    | Idle
    | Loading
    | ServerError of string

type RSSState =
    { ServerState: ServerState
      RSSList: RSS seq }

type User = { UserId: string; SessionId: string }

type UserState =
    { ServerState: ServerState
      LoginError: string option
      User: User option }

type State =
    { Search: Search.State
      RssState: RSSState
      UserState: UserState
      LoginFormState: LoginForm }

type BrowserRoute = Search of string option

type LoginFormField =
    | Username
    | Password

type Msg =
    | SearchMsg of Search.Msg
    | SaveUrls
    | GotRSSList of RSS seq
    | RssErrorMsg of exn
    | SetLoginFormValue of (LoginFormField * string)
    | InitUser
    | Login
    | GotLogin of LoginResponse option
    | UserErrorMsg of exn
    | Logout

/// Copy from https://github.com/Dzoukr/Yobo/blob/master/src/Yobo.Client/TokenStorage.fs
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

let rpcStore =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.routeBuilder
    |> Remoting.buildProxy<IRPCStore>

let getRSSList (urls: string array) =
    async { return! rpcStore.getRSSList urls }

let loginOrRegister (loginForm: LoginForm) =
    async { return! rpcStore.loginOrRegister loginForm }

let saveRSSUrlssss (userId: string, rssUrls: string array) =
    async { do! rpcStore.saveRSSUrls (userId, rssUrls) }

let initLogin (sessionId: string) =
    async { return! rpcStore.initLogin sessionId }

let route = oneOf [ map Search (top <?> stringParam "url") ]

let getUrlSearch (route: BrowserRoute option) =
    match route with
    | Some(Search(query)) ->
        match query with
        | Some query -> query.Split [| ',' |] |> Array.distinct
        | None -> [||]
    | _ -> [||]


let changeLoginFormState (state: LoginForm) (fieldName: LoginFormField) (value: string) =
    match fieldName with
    | Username -> { state with Username = value }
    | Password -> { state with Password = value }

let cmdGetRssList (urls: string array) =
    match urls with
    | [||] -> Cmd.none
    | _ -> Cmd.OfAsync.either getRSSList urls GotRSSList RssErrorMsg

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

    let loginFormState = { Username = ""; Password = "" }

    let userState =
        { User = None
          ServerState = Idle
          LoginError = None }

    { Search = searchState
      RssState = rssState
      UserState = userState
      LoginFormState = loginFormState },
    Cmd.batch [ cmdGetRssList initUrls; Cmd.ofMsg InitUser; Cmd.map SearchMsg searchCmd ]

let update (msg: Msg) (state: State) =
    match msg with
    | SearchMsg(searchMsg: Search.Msg) ->
        let nextSearchState, nextSearchCmd = Search.update searchMsg state.Search
        { state with Search = nextSearchState }, Cmd.map SearchMsg nextSearchCmd
    | SaveUrls ->
        state,
        match state.UserState.User with
        | Some user -> Cmd.OfAsync.attempt saveRSSUrlssss (user.UserId, state.Search.Urls) RssErrorMsg
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
    | SetLoginFormValue(fieldName, value) ->
        { state with
            LoginFormState = changeLoginFormState state.LoginFormState fieldName value },
        Cmd.none
    | Login ->
        let isFormFilled =
            state.LoginFormState.Username <> "" && state.LoginFormState.Password <> ""

        { state with
            UserState =
                if isFormFilled then
                    { state.UserState with
                        ServerState = Loading }
                else
                    { state.UserState with
                        LoginError = Some "Fields are requried!" } },
        if isFormFilled then
            Cmd.OfAsync.either loginOrRegister state.LoginFormState GotLogin UserErrorMsg
        else
            Cmd.none
    | InitUser ->
        match tryGetSessionId () with
        | None -> state, Cmd.none
        | Some sessionId -> state, Cmd.OfAsync.either initLogin sessionId GotLogin UserErrorMsg
    | GotLogin loginResponse ->
        match loginResponse with
        | Some user ->
            let newLoginFormState = { Username = ""; Password = "" }

            let newUser =
                { UserId = user.UserId
                  SessionId = user.SessionId }

            let newUserState =
                { ServerState = Idle
                  LoginError = None
                  User = Some newUser }

            setSessionId user.SessionId

            { state with
                LoginFormState = newLoginFormState
                UserState = newUserState },
            Cmd.ofMsg (SearchMsg(Search.Msg.SetUrls user.RssUrls))
        | None -> state, Cmd.none
    | UserErrorMsg e ->
        { state with
            RssState =
                { state.RssState with
                    ServerState = ServerError e.Message } },
        Cmd.none
    | Logout ->
        removeSessionId ()

        { state with
            UserState =
                { User = None
                  ServerState = Idle
                  LoginError = None } },
        Cmd.none

let render (state: State) (dispatch: Msg -> unit) : Fable.React.ReactElement =
    Html.div
        [ prop.className "max-w-[768px] p-5 mx-auto flex flex-col gap-3"
          prop.children
              [ Daisy.navbar
                    [ prop.className "mb-2 shadow-lg bg-neutral text-neutral-content rounded-box"
                      prop.children
                          [ Daisy.navbarStart [ Html.h1 [ prop.text "RSS Bookmarkr" ] ]
                            Daisy.navbarEnd
                                [ match state.UserState.User with
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

                Search.render state.Search (SearchMsg >> dispatch)

                if state.Search.Urls.Length <> 0 && state.UserState.User <> None then
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

                            match state.UserState.LoginError with
                            | Some errorMsg -> Daisy.alert [ alert.error; prop.text errorMsg ]
                            | None -> () ] ]
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
                                                          prop.value state.LoginFormState.Username
                                                          prop.onChange (fun value ->
                                                              dispatch (SetLoginFormValue(Username, value))) ] ]
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
                                                          prop.value state.LoginFormState.Password
                                                          prop.onChange (fun value ->
                                                              dispatch (SetLoginFormValue(Password, value))) ] ]
                                              Html.p "Account will be automatically created if not exist."
                                              Daisy.modalAction
                                                  [ Daisy.button.label
                                                        [ prop.htmlFor "login-modal"; prop.text "Cancel" ]
                                                    Daisy.button.label
                                                        [ button.neutral
                                                          prop.htmlFor "login-modal"
                                                          prop.text "Log In"
                                                          prop.type' "submit"
                                                          prop.onClick (fun _ -> dispatch Login) ] ] ] ] ] ] ] ] ]

Program.mkProgram init update render
|> Program.toNavigable (parsePath route) urlUpdate
|> Program.withReactSynchronous "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
