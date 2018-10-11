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



    type WorkItemService(sessionService :SessionService, tfs: TfsService, upd : UpdateService) = 
    

        member this.GetWorkItemStats kind sprint = 
        
            let justKind = upd.WorkItemsList 
                                  |> List.filter(fun x -> x.fields.``System.WorkItemType`` = kind)
                                  |> List.filter(fun x -> x.children |> List.ofArray
                                                                     |> List.filter(fun y -> y.fields.``System.IterationPath`` = sprint.path) 
                                                                     |> List.length > 0)
            
            let sItems = justKind 
                           |> List.map(fun x -> 
                                                 let itemCompleted = x.children |> List.ofArray
                                                                                |> List.sumBy(fun y -> y.fields.``Microsoft.VSTS.Scheduling.CompletedWork``) 
                                                 let itemOriginal  = x.children |> List.ofArray 
                                                                                |> List.sumBy(fun y -> y.fields.``Microsoft.VSTS.Scheduling.OriginalEstimate``)           
                                                 let itemRemaining =  x.children |> List.ofArray
                                                                                 |> List.sumBy(fun y -> y.fields.``Microsoft.VSTS.Scheduling.RemainingWork``) 
                                             
                                                 let iterCompleted = x.children |> List.ofArray 
                                                                                |> List.filter(fun y -> y.fields.``System.IterationPath`` = sprint.path)
                                                                                |> List.sumBy(fun y -> y.fields.``Microsoft.VSTS.Scheduling.CompletedWork``) 
                                                 
                                                 let iterOriginal = x.children |> List.ofArray 
                                                                               |> List.filter(fun y -> y.fields.``System.IterationPath`` = sprint.path)
                                                                               |> List.sumBy(fun y -> y.fields.``Microsoft.VSTS.Scheduling.OriginalEstimate``) 
                                                 
                                                 let iterRemaining = x.children |> List.ofArray 
                                                                                |> List.filter(fun y -> y.fields.``System.IterationPath`` = sprint.path)
                                                                                |> List.sumBy(fun y -> y.fields.``Microsoft.VSTS.Scheduling.RemainingWork``) 
                                                                

                                                 {  item = x
                                                    Effort= x.fields.``Microsoft.VSTS.Scheduling.Effort``

                                                    ItemStats = {Completed = itemCompleted
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

                                                    IterStats = {Completed = iterCompleted
                                                                 Original =  iterOriginal
                                                                 Remaining = iterRemaining
                                                                 }

                                                    IterStatsCmp = { Deviation = 
                                                                        x.fields.``Microsoft.VSTS.Scheduling.Effort`` |> function
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
                           lastUpdate = upd.LastUpdate}
            result

        member this.GetGlobalWorkStats kind sprint = 

            let full= this.GetWorkItemStats kind sprint
            let items = full.workItems |> List.ofArray

            let stateCount = items  |> List.groupBy(fun x -> x.item.fields.``System.State``)
                                    |> List.map(fun (x,y)-> {
                                                        name = x
                                                        count =y.Length
                                                      })
                                    

            { kind = kind
              Effort = items |> List.sumBy(fun x -> x.Effort)
              Stats= { Completed = items |> List.sumBy(fun x -> x.IterStats.Completed)
                       Remaining = items |> List.sumBy(fun x -> x.IterStats.Remaining)
                       Original = items |> List.sumBy(fun x -> x.IterStats.Original)
                     }
              StatsCmp = {Progress = items |> List.averageBy(fun x -> x.IterStatsCmp.Progress)
                          Deviation = items |> List.averageBy(fun x -> x.IterStatsCmp.Deviation)
                         }
              StateCount = stateCount |> Array.ofList
              Count = items.Length
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
