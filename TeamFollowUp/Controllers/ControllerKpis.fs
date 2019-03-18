namespace aspnetcore3.Controllers

open Microsoft.AspNetCore.Routing
open WorkItem
open Update
open Microsoft.AspNetCore.Mvc
open Newtonsoft.Json
open Tfs
open System
open TeamFollow


type KpiItem = {

    sprint : Sprint
    summary : wGlobalStats
    lastUpdate : DateTime
}

[<Route("api/kpi")>]
type KpiController(update : UpdateService, team: TeamService, workitems : WorkItemService) =
    inherit ControllerBase()
    

    [<Route("TeamComplete")>]
    member this.Team() = 
                
        update.SprintsList    |> List.map( fun current -> current
                                                          |> team.ComputeSummary)
                              |> JsonResult :> ActionResult

    [<Route("MemberComplete")>]
    member this.Member() = 
                
        update.SprintsList    |> List.map( fun current -> current
                                                          |> team.compute)
                              |> JsonResult :> ActionResult

    [<Route("WorkItemComplete")>]
    member this.WorkItems (wtype:string) =
       
        update.SprintsList  
        |> List.map (fun x -> workitems.GetWorkItemStats wtype x)
        |> JsonResult :> ActionResult
    
 