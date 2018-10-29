module TimeSeries
open FSharp.Control.Reactive
open System
open FSharp.Control.Reactive


type datapoint = {
    t : DateTimeOffset
    y : double
}

let genObservable scheduler (input:datapoint list)  = 
    Observable.generateTimedOn scheduler input (fun x -> x <> [])
                                               (fun x -> x.Tail)
                                               (fun x -> x.Head)
                                               (fun x -> x.Head.t )


let serieStartWith scheduler serie = 
        serie |> Observable.startWithOn scheduler ([{ t= DateTimeOffset.MinValue; y= 0.0}]|> Seq.ofList)  

let sumSeries (dateFrom:DateTimeOffset) dateTo (sampleTime:TimeSpan) (series:datapoint list list) = 
     
    let mutable resultList = []

    let sched = Scheduler.Scheduler.Historical
    
    let tempDate = dateFrom-(sampleTime+sampleTime)
    sched.AdvanceTo(tempDate)

    let sam =  Observable.intervalOn sched (sampleTime)
                    |> Observable.map (fun x-> {t = sched.Clock; y = 0.0})
      
    let serie = series
                |> List.map(fun x -> genObservable sched x)
                |> List.map(fun x -> serieStartWith sched x)

    let obs = sam::serie
               |> Seq.ofList
               |> Observable.combineLatestSeqMap (fun x -> { t= (x|> (Seq.maxBy(fun y -> y.t))).t
                                                             y=  x|> Seq.sumBy(fun y -> y.y)})  
               |> Observable.throttleOn sched (TimeSpan.FromMilliseconds(1.0))
               |> Observable.sampleOn sched (sampleTime)
               |> Observable.map (fun x -> { t= sched.Clock
                                             y=  x.y} )
               |> Observable.subscribeOn sched
               |> Observable.subscribe (fun x -> resultList <- x::resultList)

    sched.AdvanceTo(dateTo)
    obs.Dispose |> ignore

    resultList |> List.rev 

let cutStartSerie dateFrom serie = 
    
    serie 
        |> List.map(fun x -> {t = x.t |> function 
                                                  | l when l<=dateFrom -> dateFrom
                                                  | o -> o
                              y=  x.y})
        |> List.groupBy(fun x -> x.t)
        |> List.map(fun (x,v) -> {y = (v |> List.last).y 
                                  t = (v |> List.last).t })

let extendEndSerie dateTo serie = 
    let last = serie |> List.last
    serie @ [{t = dateTo; y = last.y}] 

                        
let cutEndSerie dateTo serie = 
    
    serie 
        |> List.map(fun x -> {t = x.t |> function | h when h>=dateTo -> dateTo
                                                  | o -> o
                              y=  x.y})
        |> List.groupBy(fun x -> x.t)
        |> List.map(fun (x,v) -> {y = (v |> List.head).y 
                                  t = (v |> List.head).t })
           

           


