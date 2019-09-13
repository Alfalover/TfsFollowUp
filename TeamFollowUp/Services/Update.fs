module Update

    open Newtonsoft.Json
    open System
    open System.IO
    open Tfs
    open Microsoft.Extensions.Configuration
    open RequestCacheFun
    open Session


    type memberCapacityRegister = {
        memberItem : memberCapacityList
        timestamp : DateTime
    }

    type teamCapacityRegister = {
        teamItem : teamCapacityList
        timestamp : DateTime
    }

    let ConvertBool (str:string) =
            match System.Boolean.TryParse(str) with
            | (true,bool) -> bool
            | _ -> false

    type UpdateService(config : IConfiguration,session :SessionService, tfs : TfsService) =
    
        let requestCacheModeTimeSpan =  (
                    ConvertBool (config.Item "tfs:UpdateDisabled")) |> function 
                                                                       | true  -> TimeSpan.MaxValue 
                                                                       | false -> TimeSpan.FromMinutes(10.0)

        let path = config.Item "folders:cache"

        let memberRCache = tfs.GetMemberCapacities session.currentSession
                           |> Cache "MemberCap" requestCacheModeTimeSpan config

        let teamRCache = tfs.GetTeamCapacities session.currentSession
                         |> Cache "TeamCap" requestCacheModeTimeSpan config 


        let revRCache = tfs.GetWorkItemRevisions session.currentSession
                        |> Cache "RevCap" requestCacheModeTimeSpan config 

        let prThreadCache = tfs.GetPullRequestsThread session.currentSession 
                            |> Cache "PRThreadCap" requestCacheModeTimeSpan config

        let prWorkItemCache = tfs.GetPullRequestsWorkItems session.currentSession 
                            |> Cache "PRWorkItemCap" requestCacheModeTimeSpan config


        // Loads a default factors of 1 on every new sprint
        let factorRequest x:memberFactorList =  
                    fst memberRCache x
                    |> fun x -> {
                                  count = x.count
                                  value = x.value 
                                             |> Array.map( fun x ->   {
                                                         teamMemberId = x.teamMember.id
                                                         factor = 1.0
                                                     }
                                              )
                                }
                        
        let factorCache = factorRequest |> Cache "FactorCap" TimeSpan.MaxValue config

        member val UpdateInProgress = false with get,set

        member val updateEvent = new Event<DateTime>()

        member val PullRequestList = List.empty<pullRequest> with get,set
        member val SprintsList =  List.empty<Sprint> with get,set
        member val WorkItemsList = List.empty<workItem> with get,set

        member val LastUpdate = DateTime.MinValue with get, set
        member val ProjectName = tfs.ProjectName
        
        member this.GetMemberCapacities sprintGuid = fst memberRCache sprintGuid  
        member this.GetTeamCapacities sprintGuid  = fst teamRCache sprintGuid
        member this.GetRevisions id = fst revRCache id
        member this.GetPRThread pr = fst prThreadCache pr
        member this.GetPRWorkItem pr = fst prWorkItemCache pr

        member this.GetTeamFactors sprintGuid = fst factorCache sprintGuid
        member this.SetTeamFactors sprintGuid newValue = snd factorCache sprintGuid newValue

        member this.performUpdate session = 
                      
                      this.UpdateInProgress <- true; 

                      let sprints = try
                                        tfs.GetSprintsList session
                                    with ex -> printfn "Error %s" (ex.ToString()); {count=0;value=[||]}

                      let pullRequests = try
                                            tfs.GetPullRequestsList session
                                         with ex -> printfn "Error %s" (ex.ToString()); {count=0;value=[||]}

                      let QueryId = config.Item "tfs:QueryId"
        
                      let wi =
                          try
                            tfs.GetWorkItemsQuery session QueryId
                          with ex -> printfn "Error %s" (ex.ToString()); {queryType="none";asOf=DateTime.MinValue;columns=[||];workItemRelations=[||]}
        
                      let wi2 =
                          try
                            tfs.GetWorkItemsList (session,wi.workItemRelations)
                          with ex -> printfn "Error %s" (ex.ToString()); []
        
                      this.SprintsList <- sprints.value |> List.ofArray
                      this.PullRequestList <- pullRequests.value |> List.ofArray
                      this.WorkItemsList <- wi2
                      this.LastUpdate <- DateTime.UtcNow
        
                      if(this.SprintsList.Length > 0) then
                          File.WriteAllText(Path.Combine(path,"SprintList.txt"),JsonConvert.SerializeObject(this.SprintsList))
                          File.WriteAllText(Path.Combine(path,"PRList.txt"),JsonConvert.SerializeObject(this.PullRequestList))
                          File.WriteAllText(Path.Combine(path,"WorkItemList.txt"),JsonConvert.SerializeObject(this.WorkItemsList))
                          File.WriteAllText(Path.Combine(path,"LastUpdate.txt"),JsonConvert.SerializeObject(this.LastUpdate))
    
                      this.updateEvent.Trigger this.LastUpdate
                      this.UpdateInProgress <- false; 
        
        member this.forceUpdate session = 
        
            if not this.UpdateInProgress then
                this.performUpdate session
            true

        member this.periodicUpdate session = async {
        
               // Load from disk last state
               try
                    let prList = JsonConvert.DeserializeObject<pullRequest list>(File.ReadAllText(Path.Combine(path,"PRList.txt")))
                    let sprintsList = JsonConvert.DeserializeObject<Sprint list>(File.ReadAllText(Path.Combine(path,"SprintList.txt")))
                    let workItemsList = JsonConvert.DeserializeObject<workItem list>(File.ReadAllText(Path.Combine(path,"WorkItemList.txt")))
                    let lastUpdate = JsonConvert.DeserializeObject<DateTime>(File.ReadAllText(Path.Combine(path,"LastUpdate.txt")))
                    this.SprintsList <- sprintsList
                    this.WorkItemsList <- workItemsList
                    this.PullRequestList <- prList
                    this.LastUpdate <- lastUpdate


               with ex ->  printfn "Unable to recover previous state"
                           this.SprintsList <- List.empty<Sprint>
                           this.WorkItemsList <- List.empty<workItem>
                           this.LastUpdate <- DateTime.MinValue
        
               this.updateEvent.Trigger this.LastUpdate
    
               let mutable continueLooping = true
               while continueLooping do
                      printfn "Update!"
        
                      // Check lastUpdate to discard request if someone use force
                      if not (ConvertBool (config.Item "tfs:UpdateDisabled")) then this.performUpdate session

                      printfn "Update ends!"
                      
                      do! Async.Sleep(1000*Convert.ToInt32(config.Item "tfs:UpdatePeriodSeconds"))
            }


