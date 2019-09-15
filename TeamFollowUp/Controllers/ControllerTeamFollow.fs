namespace aspnetcore3.Controllers
 
open Microsoft.AspNetCore.Mvc
open Update
open Newtonsoft.Json
open Tfs
open WorkItem
open Capacity
open System
open Session
open TeamFollow

[<Route("api/TeamFollow")>]
type TeamFollowController( capacities : CapacityService, update : UpdateService, team : TeamService) =
    inherit ControllerBase()


    [<Route("CapacitySerie")>]
    member this.CapacitySerie() : ActionResult  = 
                
        update.SprintsList  |> List.tryFind(fun x -> x.attributes.timeFrame = TimeFrame.current)
                            |> function 
                                | Some current -> capacities.GetCapacitySerie current
                                                  |> List.map (fun x -> x.RemCap)
                                                  |> TimeSeries.sumSeries (DateTimeOffset(current.attributes.startDate.Value))
                                                                          (DateTimeOffset(current.attributes.finishDate.Value.AddDays(1.0)))
                                                                          (TimeSpan.FromHours(24.0))
                                                  |> TimeSeries.removeWeekends
                                                  |> TimeSeries.removeNights 
                                                  |> fun x -> new JsonResult(x) :> ActionResult  

                                | None -> new EmptyResult() :> ActionResult

    [<Route("Summary")>]
    member this.Summary() : ActionResult  = 
                
        update.SprintsList  |> List.tryFind(fun x -> x.attributes.timeFrame = TimeFrame.current)
                            |> function 
                                | Some current -> team.ComputeSummary current
                                                  |> function 
                                                     | Some x -> new JsonResult(x)  :> ActionResult
                                                     | None ->  new EmptyResult() :> ActionResult
                                | None -> new EmptyResult() :> ActionResult


    [<Route("PrSummary")>]
    member this.PrSummary() : ActionResult  = 
                
        update.SprintsList  |> List.tryFind(fun x -> x.attributes.timeFrame = TimeFrame.current)
                            |> function 
                                | Some current -> team.ComputePullRequestReviewSummary current
                                                  |> function 
                                                     | Some x -> new JsonResult(x)  :> ActionResult
                                                     | None ->  new EmptyResult() :> ActionResult
                                | None -> new EmptyResult() :> ActionResult

    [<Route("PrSummaryB")>]
    member this.PrSummaryB() : ActionResult  = 
                
        update.SprintsList  |> List.tryFind(fun x -> x.attributes.timeFrame = TimeFrame.current)
                            |> function 
                                | Some current -> team.ComputePullRequestCreatedSummary current
                                                  |> function 
                                                     | Some x -> new JsonResult(x)  :> ActionResult
                                                     | None ->  new EmptyResult() :> ActionResult
                                | None -> new EmptyResult() :> ActionResult

    

    [<HttpGet>]
    member this.Get() : ActionResult = 
 
        update.SprintsList  |> List.tryFind(fun x -> x.attributes.timeFrame = TimeFrame.current)
                                                     |> function 
                                                        | Some x-> new JsonResult((team.compute x)) :> ActionResult
                                                        | None -> new EmptyResult() :> ActionResult
                                                        
                                                     

   