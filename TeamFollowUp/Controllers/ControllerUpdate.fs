namespace aspnetcore3.Controllers

open Microsoft.AspNetCore.Mvc
open Update
open Session
open Tfs
open Microsoft.Extensions.Configuration

type extradef={
                UseGifRanking : bool 
                ShowDocker :bool
                ShowNetCore: bool
              }

[<Route("api/Update")>]
type UpdateController(sessionService: SessionService, update : UpdateService, config : IConfiguration) =
    inherit ControllerBase()
 
    [<Route("State")>]
    member this.State() = 
        update.UpdateInProgress
        |> JsonResult :> ActionResult

    [<Route("Force")>]
    member this.Force() = 
        update.forceUpdate sessionService.currentSession
        |> JsonResult :> ActionResult

    [<Route("Extra")>]
     member this.Extra() = 
         {
           UseGifRanking = ConvertBool (config.Item "extra:ShowNetCore")
           ShowDocker = ConvertBool (config.Item "extra:ShowDocker")
           ShowNetCore = ConvertBool (config.Item "extra:UseGifRanking")
         }
         |> JsonResult :> ActionResult

        
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
