namespace Fable.Core

type EmitAttribute(macro: string) =
    inherit System.Attribute()

namespace Fable

module PolyglotProvider =

    open System.Text.RegularExpressions
    open FSharp.Quotations
    open FSharp.Core.CompilerServices
    open ProviderImplementation.ProvidedTypes

    open ProviderDsl
    open Fable.SimpleHttp
    open Fable.Core

    open Browser
    open Browser.Types

    let firstToUpper (s: string) =
        s.[0..0].ToUpper() + s.[1..]

    let getterCode name =
         //  always return root object
         fun (args: Expr list) -> <@@ %%args.Head @@>

    [<Emit("((x,a)=>{let o=(x||{});o[a[0]]=a[1];return o})($1,$0)")>]
    let addkv kv o = obj()

    [<Emit("$1[$0]")>]
    let getter x self= obj()

    [<Emit("$2[$0] = $1")>]
    let setter x v self = ()

    [<Emit("(new Object())")>]
    let createObj () = obj()

    let createOptionType typeName members =
        let properties =
            members
            |> List.map (fun name ->
                    if name = "smart_count" then
                        PropertyGetSet (
                            name
                            , Any
                            , false
                            , fun args -> <@@ getter name (%%args.[0] : obj) @@>
                            , fun args -> <@@ setter name (%%args.[1] : obj) %%args.[0] @@>
                        )
                    else
                        PropertyGetSet (
                            name
                            , String
                            , false
                            , fun args -> <@@ getter name (%%args.[0]: obj) |> string @@>
                            , fun args -> <@@ setter name (%%args.[1]: string) (%%args.[0]: obj) @@>
                        )
                )
        makeCustomType(typeName, properties)

    let callAction prm (x:System.Action<'a>) =
        x.Invoke(prm)

    let rec makeMember isRoot (ns:string) (name, json) =
        let path = if ns.Length > 0 then ns + "." + name else name
        match json with
        | JsonParser.Null -> []
        | JsonParser.Bool _ -> []
        | JsonParser.Number _ -> []
        | JsonParser.String value ->
            let memberName = name

            let parameterNames =
                Regex.Matches(value, "%{(.*?)}", RegexOptions.IgnoreCase)
                |> Seq.cast<System.Text.RegularExpressions.Match>
                |> Seq.map (fun m -> m.Groups.[1].Value )
                |> Seq.distinct
                |> Seq.toList

            let hasMultipleTranslations =
                value.Contains("||||")

            let hasSmartCount = parameterNames |> Seq.contains "smart_count"
            match hasSmartCount, parameterNames.Length with
            | false, 0 when hasMultipleTranslations ->
                [ Method(memberName, [ "smart_count", Any ], String, false,
                    (fun args -> <@@ ((%%args.[0] : obj) :?> Polyglot).t(path, (%%args.[1] : obj)) @@>) ) ]
            | true, 1 ->
                [ Method(memberName, [ "smart_count", Any ], String, false,
                    (fun args -> <@@ ((%%args.[0] : obj) :?> Polyglot).t(path, (%%args.[1] : obj)) @@>) ) ]
            | _, 0
            | true, 0 ->
                [ Property(memberName, String, false,
                    (fun args -> <@@ ((%%args.[0] : obj) :?> Polyglot).t(path) @@>) ) ]
            | _, _ ->

                let optionsType = createOptionType (firstToUpper name) parameterNames
                let funcType = typedefof<System.Func<_,_>>
                let optionsModifierType = funcType.MakeGenericType([| optionsType; optionsType |])

                let methodOpt =
                    Method(
                        memberName + "Opt",
                        [ "options", Custom (optionsModifierType) ]
                        , String, false,
                        fun args -> <@@ ((%%(args.[0]) : obj) :?> Polyglot).t(path,(%%args.[1] : System.Func<obj, obj>).Invoke( createObj() )) @@>)

                let args =
                    parameterNames
                    |> Seq.map (function
                        | "smart_count" -> "smart_count", Any
                        | x -> x, String )
                    |> Seq.toList

                [ ChildType optionsType
                  methodOpt
                  Method(
                    memberName, args, String, false,
                    (fun args ->

                        let fields =
                            args.Tail
                            |> List.mapi (fun i arg ->
                                match parameterNames.[i] with
                                | "smart_count" -> <@  [| box "smart_count"; (%%arg : obj ) |]@>
                                | x -> <@ [| box x; (%%arg : System.String ) :> obj |] @>
                                )

                        let prms =
                            fields
                            |> List.fold (fun state e -> <@ addkv %e %state @>) <@ null @>

                        <@@ ((%%(args.[0]) : obj) :?> Polyglot).t(path, %prms) @@>
                        )
                    )]

        | JsonParser.Array _ -> []
        | JsonParser.Object members ->
            let typeName = firstToUpper name
            let members = members |> List.collect (makeMember false path)
            let t = makeCustomType(typeName, members)
            [ ChildType t
              Property(name, Custom t, false, getterCode name ) ]

    let parseJson asm ns typeName sample =
        let makeRootType basicMembers =
            makeRootType(asm, ns, typeName, [
                yield! basicMembers |> List.collect (makeMember true "")
                yield Constructor(
                    ["phrases", Any],
                    fun args -> <@@ Polyglot.Create(%%args.[0]) @@>)
                yield Constructor(
                    [ "phrases", Any
                      "locale", String],
                    fun args -> <@@ Polyglot.Create(%%args.[0], %%args.[1]) @@>)
            ])
        match JsonParser.parse sample with
        | Some(JsonParser.Object members) ->
            makeRootType members |> Some
        | _ -> None

    [<TypeProvider>]
    type public PolyglotProvider (config : TypeProviderConfig) as this =
        inherit TypeProviderForNamespaces (config)
        let asm = System.Reflection.Assembly.GetExecutingAssembly()
        let ns = "Fable.PolyglotProvider"

        let staticParams = [ProvidedStaticParameter("phrasesFile",typeof<string>)]
        let generator = ProvidedTypeDefinition(asm, ns, "Generator", Some typeof<obj>, isErased = true)

        do generator.DefineStaticParameters(
            parameters = staticParams,
            instantiationFunction = (fun typeName pVals ->
                    match pVals with
                    | [| :? string as arg|] ->

                        if Regex.IsMatch(arg, "^https?://") then
                            async {
                                let! (status, res) = Http.get arg
                                if status <> 200 then
                                    return failwithf "URL %s returned %i status code" arg status
                                return
                                    match parseJson asm ns typeName res with
                                    | Some t -> t
                                    | None -> failwithf "Response from URL %s is not a valid JSON: %s" arg res
                            } |> Async.RunSynchronously
                        else
                            let content =
                                if arg.StartsWith("{") || arg.StartsWith("[") then arg
                                else
                                    let filepath =
                                        if System.IO.Path.IsPathRooted arg then
                                            arg
                                        else
                                            System.IO.Path.GetFullPath(System.IO.Path.Combine(config.ResolutionFolder, arg))

                                    System.IO.File.ReadAllText(filepath,System.Text.Encoding.UTF8)

                            match parseJson asm ns typeName content with
                            | Some t -> t
                            | None -> failwithf "Local sample is not a valid JSON"
                    | _ -> failwith "unexpected parameter values"
                )
            )

        do this.AddNamespace(ns, [generator])

    [<assembly:TypeProviderAssembly>]
    do ()