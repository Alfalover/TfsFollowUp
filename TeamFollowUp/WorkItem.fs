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

    type WorkItemStats = {
        
        item : workItem
        Effort: double
        ItemStats: workStats
        IterStats: workStats
        CrosIter: bool
    }

    type workItemSprintStats = {
        selSprint: Sprint
        workItems : WorkItemStats[]
        lastUpdate: DateTime
    }


    type WorkItemService(sessionService :SessionService, tfs: TfsService, upd : UpdateService) = 
    
        member val temp = 0 with get,set

        member this.GetWorkItemStats(sprint: Sprint, kind: string) = 
            let wItems = upd.WorkItemsList

            
            let justKind = wItems |> List.filter(fun x -> x.fields.``System.WorkItemType`` = kind)
                                  |> List.filter(fun x -> x.children |> List.ofArray
                                                                     |> List.filter(fun y -> y.fields.``System.IterationPath`` = sprint.path) 
                                                                     |> List.length > 0)
            
            let sItems = justKind 
                           |> List.map(fun x -> {item = x
                                                 Effort= x.fields.``Microsoft.VSTS.Scheduling.Effort``
                                                 ItemStats = {Completed = x.children |> List.ofArray
                                                                                     |> List.map(fun y -> y.fields.``Microsoft.VSTS.Scheduling.CompletedWork``) 
                                                                                     |> List.sum
                                                              Original = x.children |> List.ofArray 
                                                                                    |> List.map(fun y -> y.fields.``Microsoft.VSTS.Scheduling.OriginalEstimate``) 
                                                                                    |> List.sum
                                                              Remaining = x.children |> List.ofArray
                                                                                     |> List.map(fun y -> y.fields.``Microsoft.VSTS.Scheduling.RemainingWork``) 
                                                                                     |> List.sum
                                                                }
                                                 IterStats = {Completed = x.children |> List.ofArray 
                                                                                     |> List.filter(fun y -> y.fields.``System.IterationPath`` = sprint.path)
                                                                                     |> List.map(fun y -> y.fields.``Microsoft.VSTS.Scheduling.CompletedWork``) 
                                                                                     |> List.sum
                                                              Original = x.children |> List.ofArray 
                                                                                    |> List.filter(fun y -> y.fields.``System.IterationPath`` = sprint.path)
                                                                                    |> List.map(fun y -> y.fields.``Microsoft.VSTS.Scheduling.OriginalEstimate``) 
                                                                                    |> List.sum
                                                              Remaining = x.children |> List.ofArray 
                                                                                     |> List.filter(fun y -> y.fields.``System.IterationPath`` = sprint.path)
                                                                                     |> List.map(fun y -> y.fields.``Microsoft.VSTS.Scheduling.RemainingWork``) 
                                                                                     |> List.sum
                                                             }
                                                 CrosIter = x.children |> List.ofArray 
                                                                       |> List.map(fun y -> y.fields.``System.IterationPath``)
                                                                       |> List.distinct |> List.length > 1
                                                    })

                                        
            let result = { selSprint=sprint
                           workItems = (sItems |> Array.ofList)
                           lastUpdate = upd.LastUpdate}
            result

        member this.GetMembersWorkStats (sprint: Sprint) =

            let wItems = upd.WorkItemsList
            
            let justTask =  [ wItems |> List.collect(fun x -> x.children |> List.ofArray);
                              wItems ] |> List.concat

            let justTask = justTask
                                  |> List.filter(fun x -> x.fields.``System.WorkItemType`` = "Task")
                                  |> List.filter(fun x -> x.fields.``System.IterationPath`` = sprint.path)
            
            
            let grouped = justTask |> List.groupBy(fun x -> x.fields.``System.AssignedTo``)


            grouped |> List.map(fun (x,y) -> {user = x
                                              Original = y |> List.map(fun z -> z.fields.``Microsoft.VSTS.Scheduling.OriginalEstimate``) |> List.sum 
                                              Remaining = y |> List.map(fun z -> z.fields.``Microsoft.VSTS.Scheduling.RemainingWork``) |> List.sum
                                              Completed = y |> List.map(fun z -> z.fields.``Microsoft.VSTS.Scheduling.CompletedWork``) |> List.sum
                                              Tasks = y
                                             })
