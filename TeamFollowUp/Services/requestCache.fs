module RequestCache

open System
open System.IO
open Newtonsoft.Json

type cacheRegister<'a> = {
        item : 'a
        timestamp : DateTime
}

type cacheHolder<'i,'k when 'k :comparison> = Map<'k, cacheRegister<'i>>


type requestCache<'item,'key when 'key :comparison>(requester,file,duration:TimeSpan) =

    let completeFileName = sprintf "%sReqCache.txt" file

    let defaultCache:cacheHolder<'item,'key>  = 
                       try 
                          JsonConvert.DeserializeObject<Map<'key, cacheRegister<'item>>>(File.ReadAllText(completeFileName))
                       with ex ->  Map.empty 
                        
    let createRequest key =
           { item= requester key 
             timestamp = DateTime.UtcNow}

    member val storingToken = false with get,set

    member private this.storeToDisk cache  = 
        let write v = async{
                            this.storingToken <- true;
                            do! Async.Sleep(2000);
                            File.WriteAllText(completeFileName,JsonConvert.SerializeObject(cache));
                            this.storingToken <- false;
                            printfn  "%s Stored..." v
                         }
        this.storingToken |> function 
                              | false -> write(completeFileName)
                                         |> Async.StartImmediateAsTask 
                                         |> ignore
                              | true ->0|> ignore
        cache

    member private this.store cache = 
        this.cache <- cache
        this.cache |> this.storeToDisk 

    member val cache = defaultCache with get,set

    member private this.performRequest key = 
            let newItem = (createRequest key)
            this.cache |> Map.add key newItem
                       |> this.store
                       |> ignore
            newItem

    member private this.search key = this.cache |> Map.tryFind(key)

    member private this.timeSearch key = this.cache |> Map.filter(fun x v -> (v.timestamp-DateTime.UtcNow)<duration ) |> Map.tryFind(key)
                  
    member this.request key = 
                this.timeSearch key |> function 
                                       | Some x -> x.item
                                       | None -> (this.performRequest key).item
                                   

   