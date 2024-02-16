module App

open Feliz
open Elmish
open Elmish.React
open Fable.Remoting.Client
open Feliz.DaisyUI
open Shared

#if DEBUG
open Elmish.Debug
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


type Msg =
    | UrlChanged of string
    | AddRSSUrl of string
    | GotRSSList of RSS seq
    | ErrorMsg of exn

let musicStore: IRSSStore =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.routeBuilder
    |> Remoting.buildProxy<IRSSStore>

let getResponse (urls: string array) =
    async { return! musicStore.getRSSList urls }

let init () =
    { ServerState = Idle
      RSSList = List.empty
      Url = ""
      Urls = Array.empty },
    Cmd.none

let update (msg: Msg) (state: State) =
    match msg with
    | UrlChanged url -> { state with Url = url }, Cmd.none
    | AddRSSUrl url ->
        match Array.exists (fun elm -> elm = url) state.Urls with
        | true -> { state with Url = "" }, Cmd.none
        | false ->
            let newUrls = Array.append state.Urls [| url |]
            { state with Urls = newUrls; Url = "" }, Cmd.OfAsync.either getResponse newUrls GotRSSList ErrorMsg
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
                                          prop.placeholder "RSS Url" ]

                                    Daisy.button.button
                                        [ button.neutral
                                          prop.onClick (fun _ -> dispatch (AddRSSUrl state.Url))
                                          prop.text "Add" ]] ]
                Html.div
                    [ prop.className "flex flex-col gap-3"
                      prop.children
                          [ yield!
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
                                                                    prop.text "Read" ] ] ] ] ] ] ] ] ]

          ]

Program.mkProgram init update render
|> Program.withReactSynchronous "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
