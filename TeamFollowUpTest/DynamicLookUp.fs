module DynamicLookUp

open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Xunit

let (?) (this : 'Source) (prop : string) : 'Result option =
  try
    let p = this.GetType().GetProperty(prop)
    Some (p.GetValue(this, null) :?> 'Result)
  with ex -> None


let get<'a> (prop : string) (object : 'a) : 'Result option =
  try
    let p = object.GetType().GetProperty(prop)
    Some (p.GetValue(object, null) :?> 'Result)
  with ex -> None
      
  
type SampleRec = { Str : string; Num : int }

[<Fact>]
let `` Dynamic member lookup Test`` ()=
   
        let field = "Str"
        let rc = { Str = "Hello world!"; Num = 42 }
        let s  = Option.defaultValue "" rc?Str
        let n  = Option.defaultValue 0 rc?Num
        let o  = get field rc
        let p  = Option.defaultValue "" rc?missing
        
        o |> function
             | Some o -> Assert.Equal(o,rc.Str)
             | None -> Assert.True false

     