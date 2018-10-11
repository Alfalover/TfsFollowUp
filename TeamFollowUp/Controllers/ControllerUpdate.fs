namespace aspnetcore3.Controllers

open Microsoft.AspNetCore.Mvc
open Update
open Newtonsoft.Json
open Session


[<Route("api/Update")>]
type UpdateController(sessionService: SessionService, update : UpdateService) =
    inherit ControllerBase()
 
    [<Route("State")>]
    member this.State() = 
        update.UpdateInProgress
        |> JsonConvert.SerializeObject

    [<Route("Force")>]
    member this.Force() = 
        update.forceUpdate sessionService.currentSession
        |> JsonConvert.SerializeObject

