# Fable.Polyglot

Type Provider for https://airbnb.io/polyglot.js/. Use internationalization json files to generate strongly typed objects with support for pluralization and string template.

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
    "cars": "%{smart_count} car |||| %{smart_count} cars",
    "template": "%{firstname} %{lastname}"
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

## Note

String templates are supported. You can fill template values by using function parameters or by using a function callback.

```fsharp
[<Literal>]
let path =__SOURCE_DIRECTORY__ + "/strings.en.json"
type I18n = Fable.PolyglotProvider.Generator<path>

// Load your phrases by importing json file as module or from Json parse
let i18n = I18n(Fable.Core.JsInterop.importDefault(__SOURCE_DIRECTORY__ + "/strings.en.json"), "en")

// Order of function parameters is defined by order of discovery in the template string.
// Ensure all parameters are filled
printfn "%s" (i18n.nav.template("John", "Doe")) // prints John Doe

// Options functions allows to fill a type with template values.
// Ensure parameter names are valid at compilte, but not if they are all set
printfn "%s" (i18n.nav.templateOpt(fun x ->
                x.firstname <- "John"
                x.lastname <- "Doe"
                x)) // prints John Doe

```