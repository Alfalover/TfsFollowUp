namespace Tfs

open System.Net
open System
open Newtonsoft.Json
open System.Security
open Session
open Microsoft.Extensions.Configuration
   
    //type tfsList<'T> = {
    //      count :int 
    //      value : 'T
    //  }

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

    type memberFactorRow = {
        teamMemberId : string
        factor       : double
    }

    type memberCapacityList = {
            count : int
            value : memberCapacityRow[]
    }

    type memberFactorList = {
            count : int
            value : memberFactorRow[]
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
        ``System.Tags``: string
        ``Microsoft.VSTS.Scheduling.Effort``: double
        ``Microsoft.VSTS.Scheduling.RemainingWork``:double
        ``Microsoft.VSTS.Common.StateChangeDate``:DateTime
        ``Microsoft.VSTS.Common.ClosedDate``:DateTime
        ``Microsoft.VSTS.Scheduling.OriginalEstimate``: double
        ``Microsoft.VSTS.Scheduling.CompletedWork``: double
        ``PBI.FinalEffort``:double
        ``MSCoW.Feature``:string
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

    type repository = {
    
        id: string
        name: string
    
    }

    type review = {
        
        displayName : string
        uniqueName : string
        id : string
        imageUrl: string
        vote : int
    
    }
    
     [<CustomEquality; CustomComparison>]
    type pullRequest =
        {
            repository : repository;
            pullRequestId : string;
            title : string;
            project : string;
            status : string;
            createdBy :TeamMember;
            creationDate : DateTime;
            closedDate : Nullable<DateTime>;
            description :string;
            isDraft: Boolean ;
            reviewers: review[];
            Html : string}

        override this.Equals(y) =match y with 
                                  | :? pullRequest as other -> (this.pullRequestId = other.pullRequestId)
                                  | _  -> false

        override this.GetHashCode() = hash this.pullRequestId

        interface System.IComparable with
            member x.CompareTo y = compare x (y :?> pullRequest)

    type pullRequestList = {
        value : pullRequest[]
        count : int
    }

    type pullRequestWorkItem = {
        id : string
        url: string
    }

    type pullRequestWorkItemList = {
        value : pullRequestWorkItem[]
        count : int
    }


    type threadComment = 
            {
                 id: int;
                 parentCommentId: int;
                 author:TeamMember;
                 content: string;
                 publishedDate: DateTime;
                 lastUpdatedDate: DateTime;
                 lastContentUpdatedDate: DateTime;
                 commentType: string;
             }

    type codePosition = {
        line:int
        offset: int
    }

    type threadContext = {
  
        filePath : string
        leftFileStart: codePosition
        leftFileEnd: codePosition
    }

    type pullRequestThread = { 
        id : string
        publishedDate: DateTime
        lastUpdatedDate: DateTime
        comments: threadComment[]
        threadContext: threadContext
        status: string
    }

    type pullRequestThreadList = {
        value: pullRequestThread[]
        count: int   
    }

    type TfsService(config : IConfiguration) = 
    
        let server = (config.Item "tfs:Url")+"/DefaultCollection/"
        let projectName = config.Item "tfs:ProjectName"
        let teamName = config.Item "tfs:TeamName"
        let repositoryId = config.Item "tfs:RepositoryId"

        let SprintsUri = System.Uri (server+projectName+teamName+"/_apis/work/teamsettings/iterations?api-version=4.1")
        let MemberCapacitiesUri sprint = System.Uri (server+projectName+teamName+"/_apis/work/teamsettings/iterations/" + sprint + "/capacities")
        let TeamCapacitiesUri   sprint = System.Uri (server+projectName+teamName+"/_apis/work/teamsettings/iterations/" + sprint + "/teamdaysoff?")
        let WorkItemsUri queryId = System.Uri (server+projectName+"/_apis/wit/wiql/"+ queryId + "?api-version=4.1")
        let WorkItemUri (ids,fields) = System.Uri (server+projectName+"/_apis/wit/workitems?ids=" + ids + "&fields=" + fields + "&api-version=4.1")
        let WorkItemUri2 (ids) = System.Uri (server+projectName+"/_apis/wit/workitems?ids=" + ids + "&api-version=4.1")
        let pullRequestUri = System.Uri (server+projectName+"/_apis/git/pullrequests?searchcriteria.status=all&api-version=4.1")
        let pullRequestThreadUri (pr: String) = 
                let parm = pr.Split [|'|'|]
                System.Uri (server+projectName+ sprintf "/_apis/git/repositories/%s/pullRequests/%s/threads?api-version=4.1" (parm.[0]) (parm.[1]))
        let pullRequestPbiUri(pr: String) = 
                let parm = pr.Split [|'|'|]
                System.Uri (server+projectName+ sprintf "/_apis/git/repositories/%s/pullRequests/%s/workitems?api-version=4.1" (parm.[0]) (parm.[1]))
       
       // Burnout detailed charts
        let WorkItemRevisionUri id = System.Uri (server+ sprintf "/_apis/wit/workItems/%d/revisions?api-version=4.1" id)

        let workItemHtmlLink (id) =  server + projectName + "/_workitems?id="+ id + "&_a=edit"
        let pullRequestLink (id) = server + projectName + (sprintf "/_git/GR0009/pullrequest/%s?_a=overview" id)

        let requestFields = "System.AreaPath,System.IterationPath,System.WorkItemType," + 
                            "System.State,System.Title,System.AssignedTo,System.CreatedDate," + 
                            "System.CreatedBy,System.ChangedDate,System.ChangedBy,Microsoft.VSTS.Scheduling.Effort," + 
                            "Microsoft.VSTS.Scheduling.RemainingWork,Microsoft.VSTS.Common.StateChangeDate," + 
                            "Microsoft.VSTS.Common.ClosedDate,Microsoft.VSTS.Scheduling.OriginalEstimate," + 
                            "Microsoft.VSTS.Scheduling.CompletedWork,PBI.FinalEffort,MSCoW.Feature,System.Tags"
                            
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

        member this.GetPullRequestsList session : pullRequestList =
            this.GetDataObject<pullRequestList>(session,pullRequestUri)
            |> fun prl  ->
                {
                    count = prl.count
                    value = prl.value |> Array.map(fun (x) -> {x with Html = (pullRequestLink x.pullRequestId) })
                }

        member this.GetPullRequestsThread session pullRequest =
            this.GetDataObject<pullRequestThreadList>(session,pullRequestThreadUri pullRequest)

        member this.GetPullRequestsWorkItems session pullRequest =
               this.GetDataObject<pullRequestWorkItemList>(session,pullRequestPbiUri pullRequest)
               


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
   


