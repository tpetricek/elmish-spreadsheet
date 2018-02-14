module Parsec

type ParseStream<'T> = int * list<'T>
type Parser<'T, 'R> = Parser of (ParseStream<'T> -> option<ParseStream<'T> * 'R>)

/// Returned by the `slot` function to create a parser slot that is filled later
type ParserSetter<'T, 'R> = 
  { Set : Parser<'T, 'R> -> unit }

/// Ignore the result of the parser
let ignore (Parser p) = Parser(fun input -> 
  p input |> Option.map (fun (i, r) -> i, ()))

/// Creates a delayed parser whose actual parser is set later
let slot () = 
  let mutable slot = None
  { Set = fun (Parser p) -> slot <- Some p },
  Parser(fun input -> slot.Value input)

/// If the input matches the specified prefix, produce the specified result
let prefix (prefix:list<'C>) result = Parser(fun (offset, input) ->
  let rec loop (word:list<'C>) input =
    match word, input with
    | c::word, i::input when c = i -> loop word input
    | [], input -> Some(input)
    | _ -> None

  match loop prefix input with
  | Some(input) -> Some((offset+List.length prefix, input), result)
  | _ -> None)

/// Parser that succeeds when either of the two arguments succeed
let (<|>) (Parser p1) (Parser p2) = Parser(fun input ->
  match p1 input with
  | Some(input, res) -> Some(input, res)
  | _ -> p2 input)

/// Run two parsers in sequence and return the result as a tuple
let (<*>) (Parser p1) (Parser p2) = Parser(fun input ->
  match p1 input with
  | Some(input, res1) ->
      match p2 input with
      | Some(input, res2) -> Some(input, (res1, res2))
      | _ -> None
  | _ -> None)

/// Transforms the result of the parser using the specified function
let map f (Parser p) = Parser(fun input -> 
  p input |> Option.map (fun (input, res) -> input, f res))

/// Run two parsers in sequence and return the result of the second one
let (<*>>) p1 p2 = p1 <*> p2 |> map snd

/// Run two parsers in sequence and return the result of the first one
let (<<*>) p1 p2 = p1 <*> p2 |> map fst

/// Succeed without consuming input
let unit res = Parser(fun input -> Some(input, res))

/// Parse using the first parser and then call a function to produce
/// next parser and parse the rest of the input with the next parser
let bind f (Parser p) = Parser(fun input ->
  match p input with
  | Some(input, res) ->
      let (Parser g) = f res
      match g input with
      | Some(input, res) -> Some(input, res)
      | _ -> None
  | _ -> None)       
  
/// Parser that tries to use a specified parser, but returns None if it fails
let optional (Parser p) = Parser(fun input ->
  match p input with
  | None -> Some(input, None)
  | Some(input, res) -> Some(input, Some res) )

/// Parser that succeeds if the input matches a predicate
let pred p = Parser(function
  | offs, c::input when p c -> Some((offs+1, input), c)
  | _ -> None)

/// Parser that succeeds if the predicate returns Some value
let choose p = Parser(function
  | offs, c::input -> p c |> Option.map (fun c -> (offs + 1, input), c)
  | _ -> None)

/// Parse zero or more repetitions using the specified parser
let zeroOrMore (Parser p) = 
  let rec loop acc input = 
    match p input with
    | Some(input, res) -> loop (res::acc) input
    | _ -> Some(input, List.rev acc)
  Parser(loop [])     

/// Parse one or more repetitions using the specified parser
let oneOrMore p = 
  (p <*> (zeroOrMore p)) 
  |> map (fun (c, cs) -> c::cs)


let anySpace = zeroOrMore (pred (fun t -> t = ' '))

let char tok = pred (fun t -> t = tok)

let separated sep p =
  p <*> zeroOrMore (sep <*> p)
  |> map (fun (a1, args) -> a1::(List.map snd args))

let separatedThen sep p1 p2 =
  p1 <*> zeroOrMore (sep <*> p2)
  |> map (fun (a1, args) -> a1::(List.map snd args))

let separatedOrEmpty sep p = 
  optional (separated sep p) 
  |> map (fun l -> defaultArg l [])

let number = pred (fun t -> t <= '9' && t >= '0')

let integer = oneOrMore number |> map (fun nums -> 
  nums |> List.fold (fun res n -> res * 10 + (int n - int '0')) 0) 

let letter = pred (fun t -> 
  (t <= 'Z' && t >= 'A') || (t <= 'z' && t >= 'a'))

let run (Parser(f)) input = 
  match f (0, List.ofSeq input) with
  | Some((i, _), res) when i = Seq.length input -> Some res
  | _ -> None
 