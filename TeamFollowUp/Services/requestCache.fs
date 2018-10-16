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

    let cache:cacheHolder<'item,'key>  = 
                       try 
                          JsonConvert.DeserializeObject<Map<'key, cacheRegister<'item>>>(File.ReadAllText(completeFileName))
                       with ex ->  Map.empty

    let store cache = 
        File.WriteAllText(completeFileName,JsonConvert.SerializeObject(cache))
        cache
                        
    let createRequest key =
           { item= requester key 
             timestamp = DateTime.UtcNow}

    let performRequest key = 
            cache |> Map.add key (createRequest key)
                  |> store
                  |> Map.find key
                  
    member this.request key = 
                cache |> Map.tryFind(key)    
                      |> function 
                         | Some x -> x.item
                         | None -> (performRequest key).item
                                   

   