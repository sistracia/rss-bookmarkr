module Views

open Giraffe.ViewEngine

let unsubsribePage =
    html
        []
        [ head
              []
              [ title [] [ str "Unsubscribe Success" ]
                style
                    []
                    [ rawText
                          "html { font-family: ui-sans-serif, system-ui, sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol', 'Noto Color Emoji'}" ] ]
          body
              [ _style
                    "width: 100%;min-height: 100vh;display: flex;flex-direction: column;justify-content: center;align-items: center;gap: 5px" ]
              [ h1 [ _style "margin: 0; margin-bottom: 15px" ] [ str "Unsubscribe Success" ]
                p [ _style "margin: 0; margin-bottom: 15px" ] [ str "See you later!" ] ] ]
