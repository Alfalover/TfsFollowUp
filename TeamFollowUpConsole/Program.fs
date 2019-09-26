// Learn more about F# at http://fsharp.org

open System
open Microsoft.Extensions.DependencyInjection
open Session
open TfsFollowUp
open System.Threading

[<EntryPoint>]
let main argv  =
    printfn "Console Start"
    //AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);
    let host = TfsFollowUpInitialize(argv)

    let closing = new AutoResetEvent(false)

    // Resolve 
    let sService = host.Services.GetService<SessionService>()   
    let session = sService.createSession
    TfsFollowUpStart host session

    Console.CancelKeyPress.Add(fun x -> printfn("Closing app...")
                                        closing.Set() |> ignore
                                        () )

    AppDomain.CurrentDomain
        .UnhandledException.Add(fun exArgs -> 
                                    printfn "Unhandled t:%s ex:%s" (exArgs.IsTerminating.ToString()) (exArgs.ExceptionObject.ToString()) )
    
    closing.WaitOne() |> ignore
    
    printfn "Console Start"
    0 

