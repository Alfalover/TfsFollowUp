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
    member this.CapacitySerie() = 
                
        update.SprintsList  |> List.tryFind(fun x -> x.attributes.timeFrame = TimeFrame.current)
                            |> function 
                                | Some current -> capacities.GetCapacitySerie current
                                                  |> List.map (fun x -> x.RemCap)
                                                  |> TimeSeries.sumSeries (DateTimeOffset(current.attributes.startDate.Value))
                                                                          (DateTimeOffset(current.attributes.finishDate.Value.AddDays(1.0)))
                                                                          (TimeSpan.FromHours(24.0))
                                                  |> TimeSeries.removeWeekends
                                                  |> TimeSeries.removeNights
                                                  |> JsonConvert.SerializeObject
                                | None -> ""

    [<Route("Summary")>]
    member this.Summary() = 
                
        update.SprintsList  |> List.tryFind(fun x -> x.attributes.timeFrame = TimeFrame.current)
                            |> function 
                                | Some current -> team.ComputeSummary current
                                                  |> function 
                                                     | Some x -> x |> JsonConvert.SerializeObject
                                                     | None -> ""
                                | None -> ""
    

    [<HttpGet>]
    member this.Get() = 
        update.SprintsList  |> List.tryFind(fun x -> x.attributes.timeFrame = TimeFrame.current)
                            |> function 
                               | Some x-> team.compute x |> JsonConvert.SerializeObject
                               | None -> ""

   