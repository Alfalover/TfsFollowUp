namespace aspnetcore3.Controllers

open Microsoft.AspNetCore.Mvc
open Update
open Newtonsoft.Json
open Session
open Tfs


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

        
    [<Route("Factor")>]
    member this.UpdateFactor (sprint:string) (tmember:string) (newFactor:double) =
        update.GetTeamFactors sprint 
            |> fun x -> {
                            memberFactorList.count = x.count
                            value = x.value 
                                    |>Array.map(fun x -> x.teamMemberId = tmember
                                                         |> function 
                                                            | true -> {x with factor=newFactor}
                                                            | false -> x )
                        }
            |> update.SetTeamFactors sprint
