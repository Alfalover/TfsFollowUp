module Update

    open Newtonsoft.Json
    open System
    open System.IO
    open Tfs
    open Microsoft.Extensions.Configuration

    let ConvertBool str =
            match System.Boolean.TryParse(str) with
            | (true,bool) -> bool
            | _ -> false

    type UpdateService(config : IConfiguration, tfs : TfsService) =
    
        member val UpdateInProgress = false with get,set

        member val updateEvent = new Event<DateTime>()
    
        member val SprintsList =  List.empty<Sprint> with get,set
        member val WorkItemsList = List.empty<workItem> with get,set
        member val LastUpdate = DateTime.MinValue with get, set
    
        member this.performUpdate session = 
                      
                      this.UpdateInProgress <- true; 

                      let sprints = try
                                        tfs.GetSprintsList session
                                    with ex -> printfn "Error %s" (ex.ToString()); {count=0;value=[||]}
        
                      let QueryId = config.Item "tfs:QueryId"
        
                      let wi =
                          try
                            tfs.GetWorkItemsQuery (session,QueryId)
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


