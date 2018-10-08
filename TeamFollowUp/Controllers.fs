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
        work : MemberWorkItemStats
}

type CompletedWork = {

    sprint : Sprint
    members : MemberFollowupStats[]
    lastUpdate : DateTime
}

type TeamSummary = {

    sprint : Sprint
    lastUpdate : DateTime
}
 
 
[<Route("api/CompletedWork")>]
type CompletedWorkController(workitems : WorkItemService, update : UpdateService) =
    inherit ControllerBase()
 
    [<HttpGet>]
    member this.Get() = 
        let current = update.SprintsList  |> List.filter(fun x -> x.attributes.timeFrame = TimeFrame.current)
        let teamWork = workitems.GetMembersWorkStats(current.Head)
        JsonConvert.SerializeObject(teamWork)

[<Route("api/TeamCapacity")>]
type TeamCapacityController(capacities : CapacityService, update : UpdateService) =
    inherit ControllerBase()
 
    [<HttpGet>]
    member this.Get() = 
        update.SprintsList  
        |> List.tryFind(fun x -> x.attributes.timeFrame = TimeFrame.current) 
        |> function
            | Some value -> value |> capacities.GetTeamMembersWork 
                                  |> JsonConvert.SerializeObject
            | None -> ""

[<Route("api/TeamFollow")>]
type TeamFollowController(workitems : WorkItemService, capacities : CapacityService, update : UpdateService) =
    inherit ControllerBase()
 
     [<Route("Summary")>]
    member this.Summary() = 
                
        update.SprintsList  |> List.tryFind(fun x -> x.attributes.timeFrame = TimeFrame.current)
                            |> function 
                                | Some current -> { sprint = current
                                                    lastUpdate = update.LastUpdate}
                                                  |> JsonConvert.SerializeObject
                                | None -> ""

    [<HttpGet>]
    member this.Get() = 
        let current = update.SprintsList  |> List.filter(fun x -> x.attributes.timeFrame = TimeFrame.current)
        let teamWork = workitems.GetMembersWorkStats current.Head 
        let list = capacities.GetTeamMembersWork current.Head 
                   |> Seq.map(fun x -> { user = x.User; 
                                                   capacity =x; 
                                                   work = teamWork 
                                                          |> List.filter(fun z -> z.user = x.User.displayName) 
                                                          |> List.head })
        { sprint = current.Head; 
          members = list |> Array.ofSeq
          lastUpdate = update.LastUpdate}

        |> JsonConvert.SerializeObject

[<Route("api/WorkFollow")>]
type WorkFollowController(workitems : WorkItemService, update : UpdateService) =
    inherit ControllerBase()
 
    [<HttpGet>]
    member this.Get(wtype:string) = 
        update.SprintsList  
        |> List.filter(fun x -> x.attributes.timeFrame = TimeFrame.current)
        |> List.head
        |>  workitems.GetWorkItemStats wtype
        |> JsonConvert.SerializeObject

[<Route("api/Update")>]
type UpdateController(sessionService: SessionService, update : UpdateService) =
    inherit ControllerBase()
 
    [<Route("State")>]
    member this.State() = 
        update.UpdateInProgress
        |> JsonConvert.SerializeObject

    [<Route("Force")>]
    member this.Force() = 
        update.forceUpdate sessionService.currentSession
        |> JsonConvert.SerializeObject


   