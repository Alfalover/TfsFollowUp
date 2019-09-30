module WorkItem

    open Tfs
    open Update
    open System
    open Session
    open Capacity

    type MemberWorkItemStats = {
        user: string
        Original  : double
        Remaining : double
        Completed : double
        Tasks : workItem list
    }

    type workStats = {
        Original : double
        Remaining : double
        Completed : double
        CompletedUnFactor: double
    }

    type workStatsComputation = {    
        Deviation: double
        Progress : double    
    }

    type WorkItemStats = {
        
        item : workItem
        Effort: double

        ItemStats: workStats
        ItemStatsCmp: workStatsComputation

        IterStats: workStats
        IterStatsCmp:  workStatsComputation

        CrosIter: bool
    }

    type workItemSprintStats = {
        selSprint: Sprint
        workItems : WorkItemStats[]
        lastUpdate: DateTime
        project: string
    }

    type stateStat= {
        name:String
        count:int
    }

    type wGlobalStats = {
        kind : string
        Effort : double
        Stats : workStats
        StatsCmp : workStatsComputation
        StateCount : stateStat[]
        Count : int
    }



    type WorkItemService(upd : UpdateService, capacities : CapacityService) = 
    
        member this.GetWorkItemStatsById id sprint =     
            upd.WorkItemsList 
                |> List.filter(fun x -> x.id = id)
                |> List.filter(fun x -> x.children |> List.ofArray
                                                   |> List.filter(fun y -> y.fields.``System.IterationPath`` = sprint.path) 
                                                   |> List.length > 0)
                |> this.MapOutput sprint   

        member this.GetWorkItemStats kind sprint =     
            let a = upd.WorkItemsList 
                    |> List.filter(fun x -> x.fields.``System.WorkItemType`` = kind)
                    
            let b = a |> List.filter(fun x -> x.children |> List.ofArray
                                                         |> List.filter(fun y -> y.fields.``System.IterationPath`` = sprint.path) 
                                                         |> List.length > 0)
            b |> this.MapOutput sprint   
                            
        member this.GetAllStats sprint =     
            upd.WorkItemsList 
                |> List.filter(fun x -> x.children |> List.ofArray
                                                   |> List.filter(fun y -> y.fields.``System.IterationPath`` = sprint.path) 
                                                   |> List.length > 0)
                |> this.MapOutput sprint      
                
        member this.GetCompletedWork wi =
            wi |> fun y -> y.fields.``Microsoft.VSTS.Scheduling.CompletedWork``

        member this.GetFactorCorrectedCompletedWork sprint wi =

            let assignedTo = wi.fields.``System.AssignedTo`` 
                            |> function 
                                |null -> None
                                |x    -> Some x


            let userFactor = assignedTo |> function 
                                           |None -> 1.0
                                           |Some x ->
                                                 capacities.GetTeamMembersWork sprint  
                                                 |> List.tryFind(fun m -> m.User.displayName.EndsWith(x))
                                                 |> fun m -> match  m with          
                                                                | Some x -> x.Stats.Factor
                                                                | None -> 1.0

            wi |> fun y -> y.fields.``Microsoft.VSTS.Scheduling.CompletedWork`` * userFactor


        member this.MapOutput sprint items=     
            let sItems = items 
                           |> List.map(fun x -> 
                                                 let itemCompleted = x.children |> List.ofArray
                                                                                |> List.sumBy(fun y -> this.GetCompletedWork y) 

                                                 let itemFactorCompleted = x.children |> List.ofArray
                                                                                      |> List.sumBy(fun y -> this.GetFactorCorrectedCompletedWork sprint y)

                                                 let itemOriginal  = x.children |> List.ofArray 
                                                                                |> List.sumBy(fun y -> y.fields.``Microsoft.VSTS.Scheduling.OriginalEstimate``)           
                                                 let itemRemaining =  x.children |> List.ofArray
                                                                                 |> List.sumBy(fun y -> y.fields.``Microsoft.VSTS.Scheduling.RemainingWork``) 
                                             
                                                 let iterCompleted = x.children |> List.ofArray 
                                                                                |> List.filter(fun y -> y.fields.``System.IterationPath`` = sprint.path)
                                                                                |> List.sumBy(fun y -> this.GetCompletedWork y) 

                                                 let iterFactorCompleted = x.children |> List.ofArray 
                                                                                      |> List.filter(fun y -> y.fields.``System.IterationPath`` = sprint.path)
                                                                                      |> List.sumBy(fun y -> this.GetFactorCorrectedCompletedWork sprint y)
                                                 
                                                 let iterOriginal = x.children |> List.ofArray 
                                                                               |> List.filter(fun y -> y.fields.``System.IterationPath`` = sprint.path)
                                                                               |> List.sumBy(fun y -> y.fields.``Microsoft.VSTS.Scheduling.OriginalEstimate``) 
                                                 
                                                 let iterRemaining = x.children |> List.ofArray 
                                                                                |> List.filter(fun y -> y.fields.``System.IterationPath`` = sprint.path)
                                                                                |> List.sumBy(fun y -> y.fields.``Microsoft.VSTS.Scheduling.RemainingWork``) 
                                                                

                                                 {  item = x
                                                    Effort= x.fields.``Microsoft.VSTS.Scheduling.Effort``

                                                    ItemStats = {Completed = itemFactorCompleted
                                                                 CompletedUnFactor = itemCompleted
                                                                 Original = itemOriginal
                                                                 Remaining = itemRemaining
                                                                }

                                                    ItemStatsCmp = { Deviation = 
                                                                        x.fields.``Microsoft.VSTS.Scheduling.Effort`` |> function
                                                                                                                         | 0.0 -> 0.0
                                                                                                                         | _ -> (100.0 *(itemRemaining+itemCompleted)/x.fields.``Microsoft.VSTS.Scheduling.Effort``)-100.0;
                                                                     Progress = itemCompleted |> function 
                                                                                                 | 0.0 -> 0.0
                                                                                                 | _   -> 100.0 *itemCompleted/(itemCompleted+itemRemaining)
                                                                   }

                                                    IterStats = {Completed = iterFactorCompleted
                                                                 CompletedUnFactor = iterCompleted
                                                                 Original =  iterOriginal
                                                                 Remaining = iterRemaining
                                                                 }

                                                    IterStatsCmp = { Deviation = 
                                                                        iterOriginal |> function
                                                                                        | 0.0 -> 0.0
                                                                                        | _ -> (100.0 *(iterRemaining+iterCompleted)/iterOriginal)-100.0;
                                                                     Progress = iterCompleted |> function 
                                                                                                 | 0.0 -> 0.0
                                                                                                 | _   -> 100.0 *iterCompleted /(iterCompleted+iterRemaining)
                                                                   }

                                                    CrosIter = x.children |> List.ofArray 
                                                                          |> List.map(fun y -> y.fields.``System.IterationPath``)
                                                                          |> List.distinct |> List.length > 1
                                                 })

                                        
            let result = { selSprint=sprint
                           workItems = (sItems |> Array.ofList)
                           lastUpdate = upd.LastUpdate
                           project = upd.ProjectName}
            result

        member this.GetGlobalWorkStats kind sprint = 

            let full= this.GetWorkItemStats kind sprint
            let items = full.workItems |> List.ofArray

            let stateCount = items  |> List.groupBy(fun x -> x.item.fields.``System.State``)
                                    |> List.map(fun (x,y)-> {
                                                        name = x
                                                        count =y.Length
                                                      })
                                    
            items |> function 
            | [] -> { kind = kind
                      Effort = 0.0
                      Stats= { Completed = 0.0
                               CompletedUnFactor = 0.0
                               Remaining = 0.0
                               Original = 0.0
                             }
                      StatsCmp = {Progress = 0.0
                                  Deviation = 0.0
                                 }
                      StateCount = [||]
                      Count = 0
                    } 
            | a -> { kind = kind
                     Effort = a |> List.sumBy(fun x -> x.Effort)
                     Stats= { Completed = a |> List.sumBy(fun x -> x.IterStats.Completed)
                              CompletedUnFactor = a |> List.sumBy(fun x -> x.IterStats.CompletedUnFactor)
                              Remaining = a |> List.sumBy(fun x -> x.IterStats.Remaining)
                              Original = a |> List.sumBy(fun x -> x.IterStats.Original)
                            }
                     StatsCmp = {Progress = a |> List.averageBy(fun x -> x.IterStatsCmp.Progress)
                                 Deviation = a |> List.averageBy(fun x -> x.IterStatsCmp.Deviation)
                                }
                     StateCount = stateCount |> Array.ofList
                     Count = a.Length
                    }

        // Unused to remove...
        member this.GetTeamWorkStats sprint = 

            [ upd.WorkItemsList |> List.collect(fun x -> x.children |> List.ofArray); upd.WorkItemsList ] 
                                |> List.concat
                                |> List.filter(fun x -> x.fields.``System.WorkItemType`` = "Task")
                                |> List.filter(fun x -> x.fields.``System.IterationPath`` = sprint.path)
                                |> List.groupBy(fun x -> true) // ????
                                |> List.map(fun (x,y) -> {
                                              user="Team"
                                              Original =  y |> List.sumBy(fun z -> z.fields.``Microsoft.VSTS.Scheduling.OriginalEstimate``) 
                                              Remaining = y |> List.sumBy(fun z -> z.fields.``Microsoft.VSTS.Scheduling.RemainingWork``) 
                                              Completed = y |> List.sumBy(fun z -> z.fields.``Microsoft.VSTS.Scheduling.CompletedWork``) 
                                              Tasks = y
                                             })
                                |> List.head

        member this.GetMembersWorkStats (sprint: Sprint) =

            [ upd.WorkItemsList |> List.collect(fun x -> x.children |> List.ofArray); upd.WorkItemsList ] 
                            |> List.concat
                            |> List.filter(fun x -> x.fields.``System.WorkItemType`` = "Task")
                            |> List.filter(fun x -> x.fields.``System.IterationPath`` = sprint.path)
                            |> List.groupBy(fun x -> x.fields.``System.AssignedTo``)
                            |> List.map(fun (x,y) -> {
                                              user = x
                                              Original =  y |> List.sumBy(fun z -> z.fields.``Microsoft.VSTS.Scheduling.OriginalEstimate``) 
                                              Remaining = y |> List.sumBy(fun z -> z.fields.``Microsoft.VSTS.Scheduling.RemainingWork``) 
                                              Completed = y |> List.sumBy(fun z -> z.fields.``Microsoft.VSTS.Scheduling.CompletedWork``) 
                                              Tasks = y
                                             })
