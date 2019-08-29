# Fable.Polyglot

F# Type Provider for Fable for https://airbnb.io/polyglot.js/.

## Testing

```shell
npm install
npm start
```

## Usage

- Install nuget package `Fable.PlyglotProvider` from Nuget.
- Load your translation model inside the provider and use exposed types to load your translations

```json
{
  "nav": {
    "cars": "%{smart_count} car |||| %{smart_count} cars"
  }
}
```

```fsharp
[<Literal>]
let path =__SOURCE_DIRECTORY__ + "/strings.en.json"
type I18n = Fable.PolyglotProvider.Generator<path>

// Load your phrases by importing json file as module or from Json parse
let i18n = I18n(Fable.Core.JsInterop.importDefault(__SOURCE_DIRECTORY__ + "/strings.en.json"), "en")

printfn "%s" i18n.nav.hello // prints Hello
printfn "%s" (i18n.nav.cars(2)) // prints 2 cars
```