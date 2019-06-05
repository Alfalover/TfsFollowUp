module RequestCacheFun

open System
open System.IO
open Newtonsoft.Json
open Microsoft.Extensions.Configuration
open FSharp.Control.Reactive
open FSharp.Control.Reactive.Observable
open System.Reactive.Subjects

type cacheRegister<'a> = {
    item : 'a
    timestamp : DateTime
}

type cacheHolder<'i,'k when 'k :comparison> = Map<'k, cacheRegister<'i>>


let getPath (config:IConfiguration) file = config.Item "folders:cache"
                                           |> fun x -> Path.Combine(x,sprintf "%sReqCache.txt" file)

let defaultCache file :cacheHolder<'item,'key>  = 
                   try 
                      JsonConvert.DeserializeObject<Map<'key, cacheRegister<'item>>>(File.ReadAllText(file))
                   with ex ->  Map.empty 


let createRegister item = { item= item
                            timestamp = DateTime.UtcNow}

let createRequest key requester =
      createRegister (requester key) 

let sb:AsyncSubject<obj*string> = Subject.async

let write fileName cache =  File.WriteAllText(fileName,JsonConvert.SerializeObject(cache));
                            printfn  "%s Stored..." fileName
         
let stbs  = sb
            |> Observable.throttle(TimeSpan.FromSeconds(2.0))
            |> Observable.subscribe(fun (c,f) -> write f c )


type requestCache<'item,'key when 'key :comparison>(requester:'key -> 'item,file,duration:TimeSpan,config : IConfiguration) =


    let completeFileName = getPath config file

    member private this.storeToDisk filename cache =         
        sb.OnNext((cache,file))
        cache

    member private this.store cache = 
        this.cache <- cache
        this.cache |> this.storeToDisk 

    member val cache = defaultCache completeFileName with get,set

    member private this.performRequest key = 
            let newItem = (createRequest key requester)
            this.cache |> Map.add key newItem
                       |> this.store
                       |> ignore
            newItem

    member private this.search key = this.cache |> Map.tryFind(key)



    member private this.timeSearch key = this.cache 
                                          |> Map.toList
                                          |> List.filter(fun (x,v) -> (DateTime.UtcNow-v.timestamp)<duration )
                                          |> List.filter(fun (x,v) -> x = key)   
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

let Cache<'k,'i when 'i :comparison> file duration config requester = 
    let rc = new requestCache<'k,'i>(requester,file,duration,config)
    rc.request                                   

   