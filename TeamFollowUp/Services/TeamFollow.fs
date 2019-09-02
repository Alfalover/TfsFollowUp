module TeamFollow

open WorkItem
open Tfs
open Capacity
open System
open Update

type MemberFollowupStats = {
        
        user :TeamMember
        capacity: MemberStats
        factor: double
        debtHours : double
        debt: double
        completeness : double
        work : MemberWorkItemStats
        CompletedFactor : double
        debtHoursFactor : double
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
    
    let computeDebt untilToday completed factor  =
                                    untilToday |> function
                                                  | 0.0 -> 0.0
                                                  | _   -> 100.0 * (untilToday-(completed*factor))/untilToday 

    let computeDebtHours untilToday completed factor  = (untilToday)-(completed*factor)

    let computeCompleteness untilToday completed factor = 
                                    untilToday |> function
                                                  | 0.0 -> 100.0
                                                  | _   -> 100.0 * ((completed*factor) / untilToday)

    member this.compute sprint : CompletedWork = 
        let current = sprint 
        let teamWork = workitems.GetMembersWorkStats current 
        let list = capacities.GetTeamMembersWork current 
                   |> Seq.map(fun x -> let userWork = teamWork  
                                                        |> List.tryFind(fun z ->  z.user.StartsWith(x.User.displayName)) 
                                                        |> Option.defaultValue {
                                                                                    user = x.User.displayName;
                                                                                    Original = 0.0;
                                                                                    Remaining = 0.0;
                                                                                    Completed = 0.0;
                                                                                    Tasks = List.empty
                                                                                }
                                       let dFactor = computeDebtHours x.Stats.CapacityUntilToday (userWork.Completed)  (x.Stats.Factor)
                                       { user = x.User; 
                                         capacity =x; 
                                         work = userWork;
                                         CompletedFactor = userWork.Completed * (x.Stats.Factor);
                                         debtHours = dFactor;
                                         debtHoursFactor = dFactor/(x.Stats.Factor);
                                         factor = x.Stats.Factor;
                                         debt = computeDebt x.Stats.CapacityUntilToday (userWork.Completed) (x.Stats.Factor)
                                         completeness = computeCompleteness x.Stats.CapacityUntilToday (userWork.Completed) (x.Stats.Factor)
                                       })
                   |> Seq.sortBy(fun x -> x.debtHoursFactor)
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
                                              Factor = x.members |> Array.averageBy(fun y ->y.capacity.Stats.Factor)
                                             }
                                  work = { Original = x.members  |> sumby (fun y -> y.work.Original)
                                           Remaining = x.members |> sumby (fun y -> y.work.Remaining)
                                           Completed = x.members |> sumby (fun y -> y.work.Completed)
                                           CompletedUnFactor = x.members |> sumby (fun y -> y.work.Completed)
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
                 

