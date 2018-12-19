// Learn more about F# at http://fsharp.org

open System
open Microsoft.Extensions.DependencyInjection
open Session
open TfsFollowUp

[<EntryPoint>]
let main argv  =
    
    let host = TfsFollowUpInitialize(argv)

    // Resolve 
    let sService = host.Services.GetService<SessionService>()   
    let session = sService.createSession
    TfsFollowUpStart host session

    Console.ReadLine() |> ignore
    0 

