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
        let current = update.SprintsList  |> List.filter(fun x -> x.attributes.timeFrame = TimeFrame.current)
        let teamWork = capacities.GetTeamMembersWork(current.Head)
        JsonConvert.SerializeObject(teamWork)

[<Route("api/TeamFollow")>]
type TeamFollowController(workitems : WorkItemService, capacities : CapacityService, update : UpdateService) =
    inherit ControllerBase()
 
    [<HttpGet>]
    member this.Get() = 
        let current = update.SprintsList  |> List.filter(fun x -> x.attributes.timeFrame = TimeFrame.current)
        let teamWork = workitems.GetMembersWorkStats(current.Head)
        let capacity = capacities.GetTeamMembersWork(current.Head)

        let list = capacity |> Seq.map(fun x -> { user = x.User; 
                                                   capacity =x; 
                                                   work = teamWork |> List.filter(fun z -> z.user = x.User.displayName) |> List.head })
        let output = { sprint = current.Head; 
                       members = list |> Array.ofSeq
                       lastUpdate = update.LastUpdate}

        JsonConvert.SerializeObject(output)

[<Route("api/WorkFollow")>]
type WorkFollowController(workitems : WorkItemService, update : UpdateService) =
    inherit ControllerBase()
 
    [<HttpGet>]
    member this.Get(wtype:string) = 
        let current = update.SprintsList  |> List.filter(fun x -> x.attributes.timeFrame = TimeFrame.current)
        let output = workitems.GetWorkItemStats(current.Head,wtype)
        JsonConvert.SerializeObject(output)

[<Route("api/Update")>]
type UpdateController(sessionService: SessionService, update : UpdateService) =
    inherit ControllerBase()
 
    [<Route("State")>]
    member this.State() = 
        let output = update.UpdateInProgress
        JsonConvert.SerializeObject(output)

    [<Route("Force")>]
    member this.Force() = 
        let output =  update.forceUpdate sessionService.currentSession
        JsonConvert.SerializeObject(output)


   