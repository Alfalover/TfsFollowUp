module Update

    open Newtonsoft.Json
    open System
    open System.IO
    open Tfs
    open Microsoft.Extensions.Configuration
    open RequestCache
    open Session


    type memberCapacityRegister = {
        memberItem : memberCapacityList
        timestamp : DateTime
    }

    type teamCapacityRegister = {
        teamItem : teamCapacityList
        timestamp : DateTime
    }

    let ConvertBool str =
            match System.Boolean.TryParse(str) with
            | (true,bool) -> bool
            | _ -> false

    type UpdateService(config : IConfiguration,session :SessionService, tfs : TfsService) =
    
        let requestCacheModeTimeSpan =  (
                    ConvertBool (config.Item "tfs:UpdateDisabled")) |> function 
                                                                       | true  -> TimeSpan.MaxValue 
                                                                       | false -> TimeSpan.FromMinutes(10.0)

        let memberRequest = fun x -> tfs.GetMemberCapacities session.currentSession x
        let memberRCache = new requestCache<memberCapacityList,string>(memberRequest,"MemberCap",requestCacheModeTimeSpan)
    
        let teamRequest = fun x -> tfs.GetTeamCapacities session.currentSession x
        let teamRCache = new requestCache<teamCapacityList,string>(teamRequest,"TeamCap",requestCacheModeTimeSpan)

        let revRequest = fun x -> tfs.GetWorkItemRevisions session.currentSession x
        let revRCache = new requestCache<workItemList,int>(revRequest,"RevCap",requestCacheModeTimeSpan)

        // Loads a default factors of 1 on every new sprint
        let factorRequest x:memberFactorList =  
                    memberRCache.request x
                    |> fun x -> {
                                  count = x.count
                                  value = x.value 
                                             |> Array.map( fun x ->   {
                                                         teamMemberId = x.teamMember.id
                                                         factor = 1.0
                                                     }
                                              )
                                }
                        
        let factorCache = new requestCache<memberFactorList,string>(factorRequest,"FactorCap",TimeSpan.MaxValue)

        member val UpdateInProgress = false with get,set

        member val updateEvent = new Event<DateTime>()

        member val SprintsList =  List.empty<Sprint> with get,set
        member val WorkItemsList = List.empty<workItem> with get,set
        member val LastUpdate = DateTime.MinValue with get, set
        member val ProjectName = tfs.ProjectName
        
        member this.GetMemberCapacities sprintGuid = memberRCache.request sprintGuid  
        member this.GetTeamCapacities sprintGuid  = teamRCache.request sprintGuid
        member this.GetRevisions id = revRCache.request id

        member this.GetTeamFactors sprintGuid = factorCache.request sprintGuid
        member this.SetTeamFactors sprintGuid newValue = factorCache.update sprintGuid newValue

        member this.performUpdate session = 
                      
                      this.UpdateInProgress <- true; 

                      let sprints = try
                                        tfs.GetSprintsList session
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
                      this.WorkItemsList <- wi2
                      this.LastUpdate <- DateTime.UtcNow
        
                      if(this.SprintsList.Length > 0) then
                          File.WriteAllText("SprintList.txt",JsonConvert.SerializeObject(this.SprintsList))
                          File.WriteAllText("WorkItemList.txt",JsonConvert.SerializeObject(this.WorkItemsList))
                          File.WriteAllText("LastUpdate.txt",JsonConvert.SerializeObject(this.LastUpdate))
    
                      this.updateEvent.Trigger this.LastUpdate
                      this.UpdateInProgress <- false; 
        
        member this.forceUpdate session = 
        
            if not this.UpdateInProgress then
                this.performUpdate session
            true

        member this.periodicUpdate session = async {
        
               // Load from disk last state
               try
                    let sprintsList = JsonConvert.DeserializeObject<Sprint list>(File.ReadAllText("SprintList.txt"))
                    let workItemsList = JsonConvert.DeserializeObject<workItem list>(File.ReadAllText("WorkItemList.txt"))
                    let lastUpdate = JsonConvert.DeserializeObject<DateTime>(File.ReadAllText("LastUpdate.txt"))
                    this.SprintsList <- sprintsList
                    this.WorkItemsList <- workItemsList
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


