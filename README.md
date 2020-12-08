Excel 365 in 100ish lines of F#
===============================

This is a minimal tutorial showing how to use [F#](http://www.fsharp.org),
[Fable](http://fable.io/) (F# to JavaScript compiler) and 
[Elmish](https://elmish.github.io/) (a library implementing the 
model-view-update architecture) to build a simple clone of Excel
running as a client-side web application.

Getting started
---------------

Here is one way to run the tutorial.
Given `elmish-spreadsheet`, a clone of this repo:
```
cd elmish-spreadsheet
mkdir run ; cd run
dotnet new fable-react-elmish
cp -Rp ../src/{main.fs,evaluator.fs,helpers} src
```
- In `run/src/App.fsproj`, replace the `Compile` lines with these from `run/../src/spreadsheet.fsproj`:
```
    <Compile Include="helpers/elmish.fs" />
    <Compile Include="helpers/parsec.fs" />
    <Compile Include="evaluator.fs" />
    <Compile Include="main.fs" />
```
- Open a web browser to `http://localhost:8080`
- Then, in the `run` dir, do this:
```
cd src
dotnet build
cd ..
npm start
```
**Note: These instructions currently produce a blank browser page. We are investigating.**

You will need to use an F# editor that works well with Fable. The recommended setup is to 
use [VS Code](https://code.visualstudio.com/) with [Ionide](http://ionide.io/).

* [dotnet SDK](https://www.microsoft.com/net/download/core) 2.0 or higher
* [node.js](https://nodejs.org) with [npm](https://www.npmjs.com/)
* [Ionide](http://ionide.io/) instructions


Following the tutorial
----------------------

### Step #1: Keeping cell state

We start with four cells and cell A1 is always selected, but you cannot actually
edit the text in the cell! This is because we do not correctly store the state 
of the textbox that the user edits.

 1. Open `main.fs` and go to `renderEditor`. You can see that we already have
    an event handler for `OnInput` which shows a message using `window.alert`.
    Change the code to trigger the `UpdateValue` event using the `trigger`
    function (the first parameter of `UpdateValue` should be the position `pos`
    and the second should be the value of the input, i.e. `e.target?value`).

 2. Open `main.fs` and go to the `update` function. This needs to handle the 
    `UpdateValue` event and calculate a new state. When we get an event 
    `UpdateValue(pos, value)`, we need to create a new `state.Cells` map and
    add a mapping from `post` to `value` (using `Map.add`)

 3. Finally, open `main.fs` and go to the `renderCell` function. Right now, this
    passes `"!"` and `"?"` to the `renderEditor` and `renderView` functions. 
    Find a value for the current cell using `Map.tryFind pos state.Cells`. You 
    can handle `None` using `Option.defaultValue` (just make the default empty)
    and pass it to `renderEditor` and `renderView`.

Now you should be able to edit the value in cell A1!

### Step #2: Selecting a cell

Now, we need to allow the user to select another cell. To do this, we will need
to track the active cell in our state and add events for selecting another cell.

 1. Find the definition of `State` in `main.fs` and add a new field 
    `Active` of type `Position option` (this keeps the selected cell position
    or `None` if no cell is selected). In the `initial` function, return `None`.

 2. To change the selected cell, we need a new type of event. Find the `Event`
    type (in `main.fs`) and add a new case `StartEdit` that carries a `Position`
    value. 

 3. Modify `update` function in `main.fs` to handle the new `StartEdit` event.
    When the event happens with `pos` as the new position to be selected, 
    return a new state with `Active` set to `Some(pos)`.

 4. Go to `renderCell` and modify the condition `pos = ('A', 1)`. Rather than
    checking that we are rendering cell A1, we need to check whether we are
    rendering the cell specified in `state.Active` (note that this is an option
    type so you need to compare against `Some(pos)` or use `Option.contains`).
    
 5. Finally, we need code that will trigger our new event. Find the `renderView`
    function in `main.fs`. This creates a `<td>` element with the cell. In the
    attributes of the element, add a handler for `OnClick` that triggers (using
    the `trigger` function) the `StartEdit(pos)` event. (The code is similar to
    `OnInput` that we already have in `renderEditor`.)   

Now you can click on cells and change their values!

### Step #3: Rendering the grid

So far, we only had 4 cells. Those are created by hand in the `view` function.
We want to change the code so that it generates cells dynamically, using the 
cell and row keys in `state.Cols` and `state.Rows`.

To do this, you can either use list comprehensions with `[ .. yield .. ]` syntax
or you can use `List.map` function. The following steps describe how to use 
`List.map`, which is easier if you are new to F# (but if you know F# already, 
feel free to use list comprehensions!)

 1. You can generate headers using `List.map`. Use `state.Cols` as the input.
    In the body of the map function, you can create a header using `header (string h)`.
    You also need to append the empty cell using `empty::headers`.
    
 2. The original `view` code defines two rows using `let cells1 = ...` and 
    `let cells2 = ...`. First, modify the body to generate cell for each column
    in `state.Cols` (just like for the headers). Next, modify the code to be a 
    function that takes a row numbe `n`.

 3. Finally, use your new `cell` function to generate a row for every single 
    row of the spreadsheet specified in `state.Rows`. If you are using `List.map`, 
    the argument will need to generate a row using `tr [] (cells r)`.

### Step #4: Evaluating equations

Finally, we need to add an evaluator for spreadsheet formulas! The `parse` 
function is already implemented (in `evaluator.fs`) so you need to add the 
evaluator and put everything together.

 1. In `renderCell`, when we are handling a cell that is not selected, we
    want to parse and evaluate the code and pass the result to `renderView`.
    First, run `parse` on the cell value (when it is `Some value`) and then
    format the result using `string`. This way, you should see what the 
    result of parsing looks like.

 2. Next, modify the code to call `parse` and then `evaluate`. Since parsing
    can fail, you'll need `Option.map` or pattern matching to do this. Also,
    the `evalaute` function takes all cells too, so you need to call it using
    `evaluate state.Cells parsed`.

 3. Finally, the code for `evaluate` in `evaluator.fs` just returns 0, 1 or 2.
    Modify this to actually evaluate the expression! For `Number`, just return 
    the number; for `Binary`, recursively evaluate `l` and `r` and then apply
    the binary operator; for `Reference`, you will need to find the value in 
    `cells`, parse it and evaluate that recursively. Do not worry about correct
    error handling. We'll fix that next!

### Step #5: Add proper error handling

The evaluator can fail when you reference a cell without a value (it will crash)
or when you reference a cell within itself (it will run into an infinite loop),
so let's fix that!
 
 1. Modify the `evaluate` function in `evaluator.fs` so that it returns `option<int>` 
    rather than just `int`. You will need to return `Some` in the `Number` case
    and propagate the `None` values correctly - the easiest way to do this is using
    `Option.bind` and `Option.map`, but you can also use pattern matching using 
    `match`.

 2. Once you modify `evaluate`, you also need to modify `renderCell` in `main.fs`
    so that it calls it correctly. If you pass `None` to `renderView`, it
    will display `#ERR` in red just like Excel.

 3. Handling recursive references is harder. We currently just get into
    an infinite loop and get a stack overflow. To handle this, you need
    to modify the `evaluate` function so that it has an additional parameter
    of type `Set<Position>` that keeps a set with all cells that we are 
    evaluating. Then, when handling `Reference`, you need to make sure that
    the referenced cell is not in this set.
