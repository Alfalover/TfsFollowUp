module TeamFollow

open WorkItem
open Tfs
open Capacity
open System
open Update

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

type TeamService(workitems : WorkItemService, capacities : CapacityService, update : UpdateService) = 

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

    member this.compute sprint : CompletedWork = 
        let current = sprint
        let teamWork = workitems.GetMembersWorkStats current 
        let list = capacities.GetTeamMembersWork current 
                   |> Seq.map(fun x -> let userWork = teamWork  
                                                        |> List.tryFind(fun z -> z.user = x.User.displayName) 
                                       { user = x.User; 
                                         capacity =x; 
                                         work = userWork
                                         debt = computeDebt x.Stats.CapacityUntilToday (defaultField (fun x -> x.Completed) userWork)
                                         completeness = computeCompleteness x.Stats.CapacityUntilToday (defaultField (fun x -> x.Completed) userWork)
                                       })
        { sprint = current; 
          members = list |> Array.ofSeq
          lastUpdate = update.LastUpdate
          project = update.ProjectName}

    member this.ComputeSummary sprint  =
                 try
                    this.compute(sprint) 
                    |> function
                    | x -> Some { sprint = x.sprint
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
                 with ex ->
                    None
                 

