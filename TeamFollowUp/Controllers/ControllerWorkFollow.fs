namespace aspnetcore3.Controllers

open Microsoft.AspNetCore.Mvc
open Update
open Newtonsoft.Json
open Tfs
open WorkItem
open System

type WorkSummary = {

    sprint : Sprint
    summary : wGlobalStats
    lastUpdate : DateTime
}

[<Route("api/WorkFollow")>]
type WorkFollowController(workitems : WorkItemService, update : UpdateService) =
    inherit ControllerBase()
    

    [<Route("Summary")>]
    member this.Summary(wtype:string) = 
                
        update.SprintsList  |> List.tryFind(fun x -> x.attributes.timeFrame = TimeFrame.current)
                            |> function 
                                | Some current -> { sprint = current
                                                    summary = current |> workitems.GetGlobalWorkStats wtype
                                                    lastUpdate = update.LastUpdate}
                                                  |> JsonConvert.SerializeObject
                                | None -> ""

 
    [<HttpGet>]
    member this.Get(wtype:string) = 
        update.SprintsList  
        |> List.filter(fun x -> x.attributes.timeFrame = TimeFrame.current)
        |> List.head
        |>  workitems.GetWorkItemStats wtype
        |> JsonConvert.SerializeObject