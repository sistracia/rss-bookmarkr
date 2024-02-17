module App

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

type ServerState =
    | Idle
    | Loading
    | ServerError of string

type State =
    { Url: string
      Urls: string array
      ServerState: ServerState
      RSSList: RSS seq }

type BrowserRoute = Search of string option

type Msg =
    | UrlChanged of string
    | SetUrlParam of string
    | RemoveUrlParam of string
    | GotRSSList of RSS seq
    | ErrorMsg of exn

let musicStore: IRSSStore =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.routeBuilder
    |> Remoting.buildProxy<IRSSStore>

let getRSSList (urls: string array) =
    async { return! musicStore.getRSSList urls }

let route = oneOf [ map Search (top <?> stringParam "url") ]

let getUrlSearch (route: BrowserRoute option) =
    match route with
    | Some(Search(query: string option)) ->
        match query with
        | Some query -> query.Split [| ',' |] |> Array.distinct
        | None -> [||]
    | _ -> [||]

let generateUrlSearch urls =
    match (urls |> String.concat ",") with
    | "" -> "/"
    | newUrl -> ("?url=" + newUrl)

let cmdGetRssList urls =
    match urls with
    | [||] -> Cmd.none
    | _ -> Cmd.OfAsync.either getRSSList urls GotRSSList ErrorMsg

let urlUpdate (route: BrowserRoute option) state =
    match route with
    | Some(Search(_)) ->
        let newUrls = (getUrlSearch route)

        let (newServerState, newRSSList) =
            match newUrls with
            | [||] -> (Idle, Seq.empty)
            | _ -> (Loading, state.RSSList)

        { state with
            Urls = newUrls
            Url = ""
            RSSList = newRSSList
            ServerState = newServerState },
        cmdGetRssList newUrls
    | None ->
        { state with
            ServerState = Idle
            RSSList = Seq.empty
            Url = ""
            Urls = Array.empty },
        Cmd.none

let init (route: BrowserRoute option) =
    let initUrls = getUrlSearch route

    { ServerState =
        match initUrls with
        | [||] -> Idle
        | _ -> Loading
      RSSList = Seq.empty
      Url = ""
      Urls = initUrls },
    cmdGetRssList initUrls

let update (msg: Msg) (state: State) =
    match msg with
    | UrlChanged url -> { state with Url = url }, Cmd.none
    | SetUrlParam url ->
        let isUrlExists = state.Urls |> Array.exists (fun (elm: string) -> elm = url)

        if url <> "" && not isUrlExists then
            state, Array.append state.Urls [| url |] |> generateUrlSearch |> Navigation.newUrl
        else
            { state with Url = "" }, Cmd.none
    | RemoveUrlParam url ->
        state,
        state.Urls
        |> Array.filter (fun (elm: string) -> elm <> url)
        |> generateUrlSearch
        |> Navigation.newUrl
    | GotRSSList response ->
        { state with
            ServerState = Idle
            RSSList = response },
        Cmd.none
    | ErrorMsg e ->
        { state with
            ServerState = ServerError e.Message },
        Cmd.none

let render (state: State) (dispatch: Msg -> unit) =
    Html.div
        [ prop.className "max-w-[768px] p-5 mx-auto flex flex-col gap-3"
          prop.children
              [ Html.div
                    [ prop.className "w-full flex flex-wrap gap-3"
                      prop.children[Daisy.input
                                        [ input.bordered
                                          prop.value state.Url
                                          prop.onChange (UrlChanged >> dispatch)
                                          prop.className "flex-1"
                                          prop.placeholder "https://overreacted.io/rss.xml" ]

                                    Daisy.button.button
                                        [ button.neutral
                                          prop.onClick (fun _ -> dispatch (SetUrlParam state.Url))
                                          prop.text "Add" ]] ]
                Html.div
                    [ prop.className "flex flex-wrap gap-3"
                      prop.children
                          [ yield!
                                [ for url in state.Urls do
                                      Html.div
                                          [ (color.bgNeutral ++ (prop.className "p-1 rounded-lg flex gap-1"))
                                            prop.children
                                                [ Daisy.button.button
                                                      [ (button.error ++ button.xs)
                                                        prop.onClick (fun _ -> dispatch (RemoveUrlParam url))
                                                        prop.text "X" ]
                                                  Html.span [ color.textNeutralContent; prop.text url ] ] ] ] ] ]
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
                            | ServerError errorMsg -> Daisy.alert [ alert.error; prop.text errorMsg ] ] ] ] ]

Program.mkProgram init update render
|> Program.toNavigable (parsePath route) urlUpdate
|> Program.withReactSynchronous "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
