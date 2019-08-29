namespace Browser.Types

open System
open Fable.Core
open System.Collections.Generic

type InterpolationOptions =
  abstract prefix: string with get, set
  abstract suffix: string with get, set

type PolyglotOptions =
  abstract phrases: IDictionary<string, string> with get, set
  abstract locale: string with get, set
  abstract allowMissing: bool with get, set
  abstract onMissingKey: (string*PolyglotOptions*string) -> string with get, set
  abstract interpolation: InterpolationOptions with get, set


type Polyglot =
  abstract has: key:string -> bool
  abstract t: key:string -> string
  abstract t: key:string * opt:obj -> string
  abstract replace: newPhrases:string -> unit
  abstract clear: unit -> unit
  abstract unset: morePhrases:string * ?prefix:string -> unit
  abstract unset: morePhrases:obj * ?prefix:string -> unit
  abstract extend: morePhrases:string * ?prefix:string -> unit
  abstract extend: morePhrases:obj * ?prefix:string -> unit
  abstract locale: ?newLocale:string -> string

type PolyglotType =
    [<Emit("new $0({ phrases: $1, locale: $2 })")>] abstract Create: phrases:obj*locale:string -> Polyglot
    [<Emit("new $0({ phrases: $1 })")>] abstract Create: phrases:obj -> Polyglot
    abstract TransformPhrase: phrase:string * substitutions:string list * locale:string -> string

namespace Browser

open Fable.Core
open Browser.Types

[<AutoOpen>]
module Polyglot =
    [<ImportDefault("node-polyglot")>]
    let [<Global>] Polyglot: PolyglotType = jsNative