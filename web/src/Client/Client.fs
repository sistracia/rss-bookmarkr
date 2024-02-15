module App

open Feliz
open Elmish
open Elmish.React
open Fable.Remoting.Client

open Shared

type ServerState =
    | Idle
    | Loading
    | ServerError of string

type State =
    { Urls: string array
      ServerState: ServerState
      RSSList: RSS seq }


type Msg =
    | GetRSSList
    | GottRSSList of RSS seq
    | ErrorMsg of exn

let musicStore: IRSSStore =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.routeBuilder
    |> Remoting.buildProxy<IRSSStore>

let getResponse (urls: string array) =
    async { return! musicStore.getRSSList urls }

let init () =
    { ServerState = Idle
      RSSList = Seq.empty
      Urls = Array.empty },
    Cmd.none

let update (msg: Msg) (state: State) =
    match msg with
    | GetRSSList -> { state with ServerState = Loading }, Cmd.OfAsync.either getResponse state.Urls GottRSSList ErrorMsg
    | GottRSSList response ->
        { state with
            ServerState = Idle
            RSSList = response },
        Cmd.none
    | ErrorMsg e ->
        { state with
            ServerState = ServerError e.Message },
        Cmd.none


let render (state: State) (dispatch: Msg -> unit) =
    Html.div [ Html.button [ prop.onClick (fun _ -> dispatch GetRSSList); prop.text "Fetch" ] ]

Program.mkProgram init update render
|> Program.withReactBatched "elmish-app"
|> Program.run
