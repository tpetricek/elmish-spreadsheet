module Elmish

open Fable.Core
open Fable.Import
open Fable.Import.Browser
open Fable.Core.JsInterop

// ------------------------------------------------------------------------------------------------
// Virutal Dom bindings
// ------------------------------------------------------------------------------------------------

module Virtualdom = 
  [<Import("h","virtual-dom")>]
  let h(arg1: string, arg2: obj, arg3: obj[]): obj = failwith "JS only"

  [<Import("diff","virtual-dom")>]
  let diff (tree1:obj) (tree2:obj): obj = failwith "JS only"

  [<Import("patch","virtual-dom")>]
  let patch (node:obj) (patches:obj): Fable.Import.Browser.Node = failwith "JS only"

  [<Import("create","virtual-dom")>]
  let createElement (e:obj): Fable.Import.Browser.Node = failwith "JS only"

// ------------------------------------------------------------------------------------------------
// F# representation of DOM and rendering using VirtualDom
// ------------------------------------------------------------------------------------------------

type DomAttribute = 
  | EventHandler of (obj -> unit)
  | Attribute of string
  | Property of string

type DomNode = 
  | Text of string
  | Element of tag:string * attributes:(string * DomAttribute)[] * children : DomNode[] 

let createTree tag args children =
    let attrs = ResizeArray<_>()
    let props = ResizeArray<_>()
    for k, v in args do
      match k, v with 
      | "style", Attribute v 
      | "style", Property v ->
          let args = v.Split(';') |> Array.map (fun a ->
            let sep = a.IndexOf(':')
            if sep > 0 then a.Substring(0, sep), box (a.Substring(sep+1))
            else a, box "" )
          props.Add ("style", JsInterop.createObj args)
      | "class", Attribute v
      | "class", Property v ->
          attrs.Add (k, box v)
      | k, Attribute v ->
          attrs.Add (k, box v)
      | k, Property v ->  
          props.Add (k, box v)
      | k, EventHandler f ->
          props.Add (k, box f)
    let attrs = JsInterop.createObj attrs
    let props = JsInterop.createObj (Seq.append ["attributes", attrs] props)
    let elem = Virtualdom.h(tag, props, children)
    elem

let rec render node = 
  match node with
  | Text(s) -> 
      box s
  | Element(tag, attrs, children) ->
      createTree tag attrs (Array.map render children)

// ------------------------------------------------------------------------------------------------
// Helpers for dynamic property access & for creating HTML elements
// ------------------------------------------------------------------------------------------------
  
[<Emit("$0[$1]")>]
let getProperty (o:obj) (s:string) = failwith "!"
 
type Dynamic() = 
  static member (?) (d:Dynamic, s:string) : Dynamic = getProperty d s

let text s = Text(s)
let (=>) k v = k, Property(v)
let (=!>) k f = k, EventHandler(fun o -> f (unbox<Dynamic> o))

type El() = 
  static member (?) (_:El, n:string) = fun a b ->
    Element(n, Array.ofList a, Array.ofList b)

let h = El()

// ------------------------------------------------------------------------------------------------
// Entry point - create event and update on trigger
// ------------------------------------------------------------------------------------------------

let app id initial r u = 
  let event = new Event<'T>()
  let trigger e = event.Trigger(e)  
  let mutable container = document.createElement("div") :> Node
  document.getElementById(id).appendChild(container) |> ignore
  let mutable tree = JsInterop.createObj []
  let mutable state = initial

  let handleEvent evt = 
    state <- match evt with Some e -> u state e | _ -> state
    let newTree = r trigger state |> render
    let patches = Virtualdom.diff tree newTree
    container <- Virtualdom.patch container patches
    tree <- newTree
  
  handleEvent None
  event.Publish.Add(Some >> handleEvent)

