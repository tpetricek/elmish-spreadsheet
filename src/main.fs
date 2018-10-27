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

type State =
  { Rows : int list
    Cols : char list
    Cells : Map<Position, string> }

// ----------------------------------------------------------------------------
// EVENT HANDLING
// ----------------------------------------------------------------------------

let update msg state = 
  state, Cmd.Empty

// ----------------------------------------------------------------------------
// RENDERING
// ----------------------------------------------------------------------------

let renderEditor (trigger:Event -> unit) pos value =
  td [ Class "selected"] [ 
    input [
      AutoFocus true
      OnInput (fun e -> Browser.window.alert(e.target?value))
      Value value ]
  ]

let renderView trigger pos (value:option<_>) = 
  td 
    [ Style (if value.IsNone then [Background "#ffb0b0"] else [Background "white"]) ] 
    [ str (Option.defaultValue "#ERR" value) ]

let renderCell trigger pos state =
  if pos = ('A', 1) then
    renderEditor trigger pos "!"
  else
    renderView trigger pos (Some "?")

let view state trigger =
  let empty = td [] []
  let header h = th [] [str h]
  let headers = [ empty; header "A"; header "B" ] 
  
  let row cells = tr [] cells
  let cells1 = 
    [ header "1"
      renderCell trigger ('A', 1) state
      renderCell trigger ('B', 1) state ]
  let cells2 = 
    [ header "2"
      renderCell trigger ('A', 2) state
      renderCell trigger ('B', 2) state ]
  let rows = 
    [ row cells1; row cells2 ]

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
    Cells = Map.empty },
  Cmd.Empty    
 
Program.mkProgram initial update view
|> Program.withReact "main"
|> Program.run
