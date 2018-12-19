namespace Tfs

open System.Net
open System
open Newtonsoft.Json
open System.Security
open Session
open Microsoft.Extensions.Configuration
   

    type TimeFrame = current=0 | future=1 | past=2

    type SprintAttributes = {
        startDate:Nullable<DateTime>
        finishDate:Nullable<DateTime>
        timeFrame:TimeFrame
    }
    
    type Sprint = {
        id : string
        name : string
        path : string
        url : string
        attributes:SprintAttributes
    }

    type TeamMember = {
        id : string
        displayName: string
        uniqueName: string
        imageUrl: string
    }

    type Activities = {
     capacityPerDay : double
     name : string
    }

    type DaysOff = {
        start: DateTime
        ``end``: DateTime
    }

    type SprintList = {
            count : int
            value : Sprint[]
        }

    type memberCapacityRow = {
        teamMember : TeamMember
        activities : Activities[]
        daysOff    : DaysOff[]
    }

    type memberCapacityList = {
            count : int
            value : memberCapacityRow[]
    }
    
    type teamCapacityList = {
            daysOff : DaysOff[]
    }

    type queryResultColumn = {
            referenceName : string
            name : string
            url: string
    }

    type workItemRelationOrigin = {
            id : string
            url : string
    }

    type workItemRelation = {
            rel : string 
            target : workItemRelationOrigin
            source : workItemRelationOrigin
    }

    type workItemsQueryList = {
            queryType: string
            asOf: DateTime
            columns : queryResultColumn[]
            workItemRelations : workItemRelation[]
    }

    type workItemFields = {

        ``System.AreaPath``: string
        ``System.IterationPath``:string
        ``System.WorkItemType``: string
        ``System.State``:string 
        ``System.Title``:string
        ``System.AssignedTo``: string
        ``System.CreatedDate``: string
        ``System.CreatedBy``: string
        ``System.ChangedDate``: string
        ``System.ChangedBy``: string
        ``Microsoft.VSTS.Scheduling.Effort``: double
        ``Microsoft.VSTS.Scheduling.RemainingWork``:double
        ``Microsoft.VSTS.Common.StateChangeDate``:DateTime
        ``Microsoft.VSTS.Common.ClosedDate``:DateTime
        ``Microsoft.VSTS.Scheduling.OriginalEstimate``: double
        ``Microsoft.VSTS.Scheduling.CompletedWork``: double
        ``PBI.FinalEffort``:double
    }

    type workItem = {
        id: string
        fields: workItemFields
        Html : string
        parentHtml: string
        parentName: string
        children: workItem[]
    }

    type workItemList = {
        count: int
        value : workItem[]
    }


    type TfsService(config : IConfiguration) = 
    
        let server = (config.Item "tfs:Url")+"/DefaultCollection/"
        let projectName = config.Item "tfs:ProjectName"

        let SprintsUri = System.Uri (server+projectName+"/_apis/work/teamsettings/iterations?api-version=4.1")
        let MemberCapacitiesUri sprint = System.Uri (server+projectName+"/_apis/work/teamsettings/iterations/" + sprint + "/capacities")
        let TeamCapacitiesUri   sprint = System.Uri (server+projectName+"/_apis/work/teamsettings/iterations/" + sprint + "/teamdaysoff?")
        let WorkItemsUri queryId = System.Uri (server+projectName+"/_apis/wit/wiql/"+ queryId + "?api-version=4.1")
        let WorkItemUri (ids,fields) = System.Uri (server+projectName+"/_apis/wit/workitems?ids=" + ids + "&fields=" + fields + "&api-version=4.1")
        let WorkItemUri2 (ids) = System.Uri (server+projectName+"/_apis/wit/workitems?ids=" + ids + "&api-version=4.1")

        // Burnout detailed charts
        let WorkItemRevisionUri id = System.Uri (server+ sprintf "/_apis/wit/workItems/%d/revisions?api-version=4.1" id)

        let workItemHtmlLink (id) =  server + projectName + "/_workitems?id="+ id + "&_a=edit"

        let requestFields = "System.AreaPath,System.IterationPath,System.WorkItemType," + 
                            "System.State,System.Title,System.AssignedTo,System.CreatedDate," + 
                            "System.CreatedBy,System.ChangedDate,System.ChangedBy,Microsoft.VSTS.Scheduling.Effort," + 
                            "Microsoft.VSTS.Scheduling.RemainingWork,Microsoft.VSTS.Common.StateChangeDate," + 
                            "Microsoft.VSTS.Common.ClosedDate,Microsoft.VSTS.Scheduling.OriginalEstimate," + 
                            "Microsoft.VSTS.Scheduling.CompletedWork,PBI.FinalEffort,MSCoW.Feature"
                            
        member this.GetDataObject<'T> (session : sessionHolder, url:Uri) = 
            printfn "REQ:%s " (url.ToString())
            let req = WebRequest.Create(url)
            req.Credentials <- session.credentials
            use resp = req.GetResponse()
            use stream = resp.GetResponseStream()
            use reader = new IO.StreamReader(stream)
            let data = reader.ReadToEnd()
            printfn "RES:%d bytes" data.Length
            JsonConvert.DeserializeObject<'T>(data)
        
        member val ProjectName =  config.Item "tfs:ProjectName"

        member this.GetSprintsList session = 
            this.GetDataObject<SprintList>(session,SprintsUri)

        member this.GetMemberCapacities session sprintGuid = 
            this.GetDataObject<memberCapacityList> (session,MemberCapacitiesUri sprintGuid)

        member this.GetTeamCapacities  session sprintGuid  = 
            this.GetDataObject<teamCapacityList> (session,TeamCapacitiesUri sprintGuid)

        member this.GetWorkItemRevisions  session id  = 
            this.GetDataObject<workItemList> (session,WorkItemRevisionUri id)
        
        member this.GetWorkItemsQuery session queryId = 
            this.GetDataObject<workItemsQueryList> (session, WorkItemsUri queryId)

        member this.GetWorkItemsList (session, relations : workItemRelation[]) =
            let relList = relations |> List.ofArray 
            
            let plain = relList 
                          |> List.map(fun x -> x.target.id) 
                          |> List.chunkBySize(100) 
                          |> List.map(fun x -> 
                                            let ids = x |> String.concat(",")
                                            this.GetDataObject<workItemList>(session, WorkItemUri (ids,requestFields))) 
                          |> List.collect(fun x -> x.value |> List.ofArray)

            let rootsIds = relList 
                            |> List.filter(fun x -> x.rel = null) 
                            |> List.map(fun x -> x.target.id)

            let endRoots = plain 
                           |> List.filter(fun x -> rootsIds |> List.contains(x.id))
                           |> List.map(fun t ->

                                     let childrenIds =  relList |> List.filter(fun x -> x.rel <> null) 
                                                                |> List.filter(fun x -> x.source.id = t.id)
                                                                |> List.map(fun x -> x.target.id)

                                     let children = plain |> List.filter(fun x -> childrenIds |> List.contains(x.id))
                                                          |> List.map(fun x -> {x with Html= workItemHtmlLink x.id
                                                                                       parentName = t.fields.``System.Title``;
                                                                                       parentHtml = workItemHtmlLink t.id})
                                     { t with children = children |> List.toArray
                                              Html = workItemHtmlLink t.id})
            
            endRoots
   


