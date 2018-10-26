module Spreadsheet

open Elmish
open Elmish.React
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Core.JsInterop

open Evaluator

// ----------------------------------------------------------------------------
// INSTRUCTIONS
// ----------------------------------------------------------------------------


// STEP #1: Rendering the grid
// ---------------------------
// Go to the `renderSheet` function. Currently, the function generates
// a static HTML table. Change the function so that it generates the table
// dynamically. There should be one column for every index in `state.Cols` 
// and one row for every index in `state.Rows`. You can do this using 
// `List.map`, or using F# list comprehensions (`for` loop and `yield`)
// To get the <td> element with body for a given cell, call the `renderCell` 
// function with the right position.


// STEP #2: Selecting a cell
// -------------------------
// The `renderCell` function currently renders an editor for cell A1 and
// displays values as text for all other cells. Now, we want to be able to 
// click on another cell to edit it. The events to do this are already 
// defined and the code to trigger them is also in-place:
//
//  - In `renderEditor`, the `onkeyup` handler triggers `FinishEdit` 
//    when the key code is [Enter]
//  - In `renderView`, the `onclick` handler triggers `StartEdit`
//
// To support selection of cells, you first need to modify the `State` type 
// and add a field that will represent the selected cell. This is optional,
// because if you finish editing one cell, no other call is selected
// (the `Position` type is a type alias for `char * int`). You need to:
//
//  1. Add `Active : option<Position>` to the `State` record
//  2. Add an initial value to `initial`
//  3. Handle `StartEdit` and `FinishEdit` in the `update` function
//  4. Check if `state.Active` matches `pos` in `renderCell`


// STEP #3: Evaluate equations!
// ----------------------------
// Currently, the `renderCell` just displays the text you enter in the editor.
// Now, we want to evaluate basic expressions like `=1+2` and `=A10*3`. 
// The parser for the expressions is already implemented in `evaluator.fs`,
// so all you need to do is to implement and call the `evaluate` function!
//
// First, go to `renderCell`. In the case when we are rendering view (not 
// the editor), you need to get the entered string from `state.Cells` and,
// if it is Some, parse it using the `parse` function and then pass the
// parsed expression to `evaluate` (these two functions are in `evaluator.fs`)
//
// Second, open `evaluator.fs`. The `evaluate` function is a recursive 
// function, but currently it just returns 0, 1 or 2. Implement the 
// evaluation function! (Don't worry about handling errors correctly, 
// we'll fix that in the next step.)


// STEP #4: Better error handling
// ------------------------------
// The evaluator can fail:
//  1. If you reference a cell without a value
//  2. If you reference a cell from within itself
//
// To handle errors, we need to modify the `evaluate` function so that
// it returns `option<int>` rather than just `int`. Go to `evaluator.fs`
// and modify the function! You will need to propagate the `None` values
// correctly - the best way to do this is using `Option.bind` and 
// `Option.map`, but you can also use pattern matching using `match`.
//
// Once you modify `evaluate` to return `option<int>`, you will need
// to modify the code in `renderCell` below. For a start, you can just
// display `#ERR`, but you can also modify `renderView` to indicate
// errors with a different background color.
//
// Handling the second case is harder, because we currently just get into
// an infinite loop and get a stack overflow. To handle this, you need
// to modify the `evaluate` function so that it has an additional parameter
// of type `Set<Position>` that keeps a set with all cells that we are 
// evaluating. Then, when handling `Reference`, you need to make sure that
// the referenced cell is not in this set.

// ----------------------------------------------------------------------------
// DOMAIN MODEL
// ----------------------------------------------------------------------------

type Event =
  | StartEdit of Position
  | UpdateValue of Position * string
  | FinishEdit 

type State =
  { Rows : int list
    Cols : char list
    Active : option<Position> 
    Cells : Map<Position, string> }

// ----------------------------------------------------------------------------
// EVENT HANDLING
// ----------------------------------------------------------------------------

let update msg state = 
  match msg with 
  | StartEdit(pos) -> 
      { state with Active = Some pos }, Cmd.Empty
  | FinishEdit ->
      { state with Active = None }, Cmd.Empty
  | UpdateValue(pos, value) ->
      { state with Cells = Map.add pos value state.Cells }, Cmd.Empty

// ----------------------------------------------------------------------------
// RENDERING
// ----------------------------------------------------------------------------

let renderEditor trigger pos value =
  td [ Class "selected"] [ 
    input [
      OnInput (fun e -> trigger (UpdateValue(pos, e.target?value)))
      OnKeyUp (fun e -> if e.keyCode = 13. then trigger FinishEdit)
      Value value ]
  ]

let renderView trigger pos (value:option<_>) = 
  td 
    [ Style (if value.IsNone then [Background "#ffb0b0"] else [Background "white"])
      OnClick (fun _ -> trigger(StartEdit(pos))) ] 
    [ str (Option.defaultValue "#ERR" value) ]

let renderCell trigger pos state =
  let value = Map.tryFind pos state.Cells |> Option.defaultValue ""
  if state.Active = Some pos then
    renderEditor trigger pos value
  else
    match Map.tryFind pos state.Cells with
    | Some cell -> 
        parse cell
        |> Option.bind (evaluate Set.empty state.Cells)
        |> Option.map string
        |> renderView trigger pos
    | _ -> 
        renderView trigger pos (Some "")

let view state trigger =
  table [] [
    tr [] [
      yield th [] []
      for col in state.Cols ->
        th [] [ str (string col) ]
    ]
    tbody [] [
      for row in state.Rows ->
        tr [] [
          yield th [] [ str (string row) ]
          for col in state.Cols -> renderCell trigger (col, row) state
        ]
    ]
  ]

// ----------------------------------------------------------------------------
// ENTRY POINT
// ----------------------------------------------------------------------------

let initial () = 
  { Cols = ['A' .. 'T']
    Rows = [1 .. 20]
    Active = None
    Cells = Map.empty },
  Cmd.Empty    
 
Program.mkProgram initial update view
|> Program.withReact "main"
|> Program.run