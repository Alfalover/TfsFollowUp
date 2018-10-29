namespace aspnetcore3.Controllers
 
open Microsoft.AspNetCore.Mvc
open Update
open Newtonsoft.Json
open Tfs
open WorkItem
open Capacity
open System
open Session


type MemberFollowupStats = {
        
        user :TeamMember
        capacity: MemberStats
        debt: double
        completeness : double
        work : MemberWorkItemStats option
}

type CompletedWork = {

    sprint : Sprint
    members : MemberFollowupStats[]
    lastUpdate : DateTime
    project: string
}

type TeamSummary = {

    sprint : Sprint
    capacity : capacityStats
    work : workStats
    debt: double
    completeness : double
    lastUpdate : DateTime
    project: string
}
 

[<Route("api/TeamFollow")>]
type TeamFollowController(workitems : WorkItemService, capacities : CapacityService, update : UpdateService) =
    inherit ControllerBase()

    let sumby y x = x   |> List.ofArray
                        |> List.sumBy(y)

    let defaultField (y:(MemberWorkItemStats -> float)) (x:MemberWorkItemStats option)  = (x |> function
                                                                                             | Some x -> y(x)
                                                                                             | None -> 0.0)
 
    let computeDebt untilToday completed  =
                                    untilToday |> function
                                                  | 0.0 -> 0.0
                                                  | _   -> 100.0 * (untilToday-completed)/untilToday 

    let computeCompleteness untilToday completed = 
                                    untilToday |> function
                                                  | 0.0 -> 100.0
                                                  | _   -> 100.0 * (completed / untilToday)

    [<Route("CapacitySerie")>]
    member this.CapacitySerie() = 
                
        update.SprintsList  |> List.tryFind(fun x -> x.attributes.timeFrame = TimeFrame.current)
                            |> function 
                                | Some current -> capacities.GetCapacitySerie current
                                                  |> List.map (fun x -> x.RemCap)
                                                  |> TimeSeries.sumSeries (DateTimeOffset(current.attributes.startDate.Value))
                                                                          (DateTimeOffset(current.attributes.finishDate.Value.AddDays(1.0)))
                                                                          (TimeSpan.FromHours(24.0))
                                                  |> JsonConvert.SerializeObject
                                | None -> ""

    [<Route("Summary")>]
    member this.Summary() = 
                
        update.SprintsList  |> List.tryFind(fun x -> x.attributes.timeFrame = TimeFrame.current)
                            |> function 
                                | Some current -> this.compute()
                                                  |>  function 
                                                      | x -> { sprint = x.sprint
                                                               capacity = {
                                                                           CapacityUntilToday = x.members |> sumby (fun y -> y.capacity.Stats.CapacityUntilToday)
                                                                           CapacitySprint = x.members |> sumby (fun y -> y.capacity.Stats.CapacitySprint)
                                                                           CapacityDay = x.members |> sumby (fun y -> y.capacity.Stats.CapacityDay)
                                                                          }
                                                               work = { Original = x.members  |> sumby (fun y -> (defaultField (fun x -> x.Original) y.work))
                                                                        Remaining = x.members |> sumby (fun y -> (defaultField (fun x -> x.Remaining) y.work))
                                                                        Completed = x.members |> sumby (fun y -> (defaultField (fun x -> x.Completed) y.work))
                                                                      }
                                                               debt = x.members |> List.ofArray
                                                                                |> List.averageBy(fun y -> y.debt)
                                                               completeness = x.members |> List.ofArray
                                                                                        |> List.averageBy(fun y -> y.completeness)
                                                               lastUpdate = x.lastUpdate
                                                               project = x.project
                                                              }

                                                  |> JsonConvert.SerializeObject
                                | None -> ""

    member this.compute() : CompletedWork = 
        let current = update.SprintsList  |> List.filter(fun x -> x.attributes.timeFrame = TimeFrame.current)
        let teamWork = workitems.GetMembersWorkStats current.Head 
        let list = capacities.GetTeamMembersWork current.Head 
                   |> Seq.map(fun x -> let userWork = teamWork  
                                                        |> List.tryFind(fun z -> z.user = x.User.displayName) 
                                       { user = x.User; 
                                         capacity =x; 
                                         work = userWork
                                         debt = computeDebt x.Stats.CapacityUntilToday (defaultField (fun x -> x.Completed) userWork)
                                         completeness = computeCompleteness x.Stats.CapacityUntilToday (defaultField (fun x -> x.Completed) userWork)
                                       })
        { sprint = current.Head; 
          members = list |> Array.ofSeq
          lastUpdate = update.LastUpdate
          project = update.ProjectName}


    [<HttpGet>]
    member this.Get() = 
        this.compute() |> JsonConvert.SerializeObject


   