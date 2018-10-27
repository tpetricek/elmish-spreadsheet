module Spreadsheet

open Elmish
open Elmish.React
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Core.JsInterop
open Fable.Import

open Evaluator

// ----------------------------------------------------------------------------
// DOMAIN MODEL
// ----------------------------------------------------------------------------

type Event =
  | UpdateValue of Position * string
  | StartEdit of Position

type State =
  { Rows : int list
    Active : Position option
    Cols : char list
    Cells : Map<Position, string> }

// ----------------------------------------------------------------------------
// EVENT HANDLING
// ----------------------------------------------------------------------------

let update msg state = 
  match msg with 
  | StartEdit(pos) ->
      { state with Active = Some pos }, Cmd.Empty

  | UpdateValue(pos, value) ->
      let newCells = Map.add pos value state.Cells
      { state with Cells = newCells }, Cmd.Empty

// ----------------------------------------------------------------------------
// RENDERING
// ----------------------------------------------------------------------------

let renderEditor (trigger:Event -> unit) pos value =
  td [ Class "selected"] [ 
    input [
      AutoFocus true
      OnInput (fun e -> trigger(UpdateValue(pos, e.target?value)))
      Value value ]
  ]

let renderView trigger pos (value:option<_>) = 
  td 
    [ Style (if value.IsNone then [Background "#ffb0b0"] else [Background "white"]) 
      OnClick (fun _ -> trigger(StartEdit(pos)) ) ] 
    [ str (Option.defaultValue "#ERR" value) ]

let renderCell trigger pos state =
  let value = Map.tryFind pos state.Cells 
  if state.Active = Some pos then
    renderEditor trigger pos (Option.defaultValue "" value)
  else
    let value = 
      match value with 
      | Some value -> 
          parse value |> Option.bind (evaluate Set.empty state.Cells) |> Option.map string
      | _ -> Some ""
    renderView trigger pos value

let view state trigger =
  let empty = td [] []
  let header h = th [] [str h]
  let headers = state.Cols |> List.map (fun h -> header (string h))
  let headers = empty::headers
  
  let row cells = tr [] cells
  let cells n = 
    let cells = state.Cols |> List.map (fun h -> renderCell trigger (h, n) state)
    header (string n) :: cells
  let rows = state.Rows |> List.map (fun r -> tr [] (cells r))

  table [] [
    tr [] headers
    tbody [] rows
  ]

// ----------------------------------------------------------------------------
// ENTRY POINT
// ----------------------------------------------------------------------------

let initial () = 
  { Cols = ['A' .. 'K']
    Rows = [1 .. 15]
    Active = None
    Cells = Map.empty },
  Cmd.Empty    
 
Program.mkProgram initial update view
|> Program.withReact "main"
|> Program.run
