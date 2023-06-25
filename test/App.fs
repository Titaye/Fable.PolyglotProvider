module App

open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Fable.SimpleHttp

[<Literal>]
let path =__SOURCE_DIRECTORY__ + "/strings.en.json"
type I18n = Fable.PolyglotProvider.Generator<path>

type Model =
  { I18nEn: I18n
    I18nFr: I18n
    Name: string
    Count: float }

type Msg =
  | NameUpdated of string
  | CountUpdated of float option

let init() : Model * Cmd<Msg> =
  { I18nEn = I18n(Fable.Core.JsInterop.importDefault("./strings.en.json"), "en")
    I18nFr = I18n(Fable.Core.JsInterop.importDefault("./strings.fr.json"), "fr")
    Name = ""
    Count = 0. }, Cmd.none

let update (msg:Msg) (model:Model) =
    match msg with
    | NameUpdated name -> { model with Name = name }, Cmd.none
    | CountUpdated cnt -> { model with Count = cnt |> Option.defaultValue 0. }, Cmd.none

let viewLng (i18n:I18n) (model:Model) dispatch =

  let par txt =
    p [] [str txt]

  div [ ] [
      div []
          [ h2 [] [ par i18n.nav.sidebar.welcome ]
          ]
      div []
          [ h2 [] [ par i18n.nav.hello ]
            input [ Type "text"
                    OnChange (fun ev -> NameUpdated ev.Value |> dispatch)
                    Value model.Name ]
            par (i18n.nav.hello_name(model.Name))
          ]
      div []
          [ h2 [] [
            par (i18n.nav.template("1", "2", "3") )
            par (i18n.nav.templateOpt(fun opt ->
              opt.a <- "10"
              opt.b <- "20"
              opt.c <- "30"
              opt
            ))
            ] ]
      div []
          [ h2 [] [ par (i18n.nav.count(model.Count) ) ]
            input [ Type "text"
                    Value model.Count
                    OnChange (fun ev ->
                      match ev.Value |> System.Double.TryParse with
                      | true, value -> CountUpdated (Some value) |> dispatch
                      | false, _ -> CountUpdated None |> dispatch) ]
            par (i18n.nav.num_cars(model.Count))
          ]
  ]

let view (model:Model) dispatch =
  div [ Style [ Display DisplayOptions.Flex
                FlexDirection "row"  ] ]
    [
      viewLng model.I18nEn model dispatch
      viewLng model.I18nFr model dispatch
    ]

// App
Program.mkProgram init update view
|> Program.withReactSynchronous "elmish-app"
|> Program.withConsoleTrace
|> Program.run
