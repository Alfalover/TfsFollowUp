module RequestCache

open System
open System.IO
open Newtonsoft.Json
open System
open Microsoft.Extensions.Configuration

type cacheRegister<'a> = {
        item : 'a
        timestamp : DateTime
}

type cacheHolder<'i,'k when 'k :comparison> = Map<'k, cacheRegister<'i>>


type requestCache<'item,'key when 'key :comparison>(requester:'key -> 'item,file,duration:TimeSpan,config : IConfiguration) =

    let listCount text (list:'a list) = 
        printfn "%s count %d" text list.Length
        list

    let path = config.Item "folders:cache"

    let completeFileName = Path.Combine(path,sprintf "%sReqCache.txt" file)

    let defaultCache:cacheHolder<'item,'key>  = 
                       try 
                          JsonConvert.DeserializeObject<Map<'key, cacheRegister<'item>>>(File.ReadAllText(completeFileName))
                       with ex ->  Map.empty 
                        
    let createRegister item = 
           { item= item
             timestamp = DateTime.UtcNow}

    let createRequest key =
          createRegister (requester key) 

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



    member private this.timeSearch key = this.cache 
                                          |> Map.toList
//                                          |> listCount "prev"
                                          |> List.filter(fun (x,v) -> (DateTime.UtcNow-v.timestamp)<duration )
//                                          |> listCount "timefilter"
                                          |> List.filter(fun (x,v) -> x = key)   
//                                          |> listCount "search"
                                          |> List.map(fun(x,v) -> v)
                                          |> List.tryItem 0
    member this.request key = 
                this.timeSearch key |> function 
                                       | Some x -> 
                                                    (printfn "%s At Cache.. %s -- %s ->%s d:%s" file (x.timestamp.ToString()) (((DateTime.UtcNow-x.timestamp).TotalMinutes).ToString())
                                                        (((DateTime.UtcNow-x.timestamp)<duration).ToString()) (duration.TotalMinutes.ToString())) ;
                                                    x.item
                                       | None -> (this.performRequest key).item
    

    member this.update key value = 
                  this.cache |> Map.add key (createRegister value)
                       |> this.store
                       |> ignore
                                   

   