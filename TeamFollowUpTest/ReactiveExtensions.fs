module ReactiveExtensions

open System
open Xunit
open FSharp.Control.Reactive
open FSharp.Control.Reactive
open System.Threading


[<Fact>]
let ``Async subject test to allow request cache repo store`` ()=

    let mutable count = 0

    let sb = Subject.broadcast

    let subs = sb  |> Observable.throttle(TimeSpan.FromSeconds(0.5))
                   |> Observable.subscribe(fun (c,f) -> count <- count + 1 )



    sb.OnNext((1,1))
    sb.OnNext((1,1))
    Thread.Sleep(1000)    
    sb.OnNext((1,1))
    sb.OnNext((1,1))
    sb.OnNext((1,1))

    Thread.Sleep(1000)

    Assert.Equal(2,count)


[<Fact>]
let ``Reactive Extensions test & learning`` ()=

     let mutable resultList = []
     let list = [1;2;3;4] |> Seq.ofList
     let sched = Scheduler.Scheduler.Historical
     
     let obs = Observable.ofSeqOn sched list

     let subs = obs |> Observable.subscribeOn sched |> Observable.subscribe (fun x -> resultList <- x::resultList)

     sched.AdvanceBy(TimeSpan.FromMinutes(10.0))

     Assert.Equal<int list>([1;2;3;4],resultList |> List.rev)

type dada = {
    t : DateTimeOffset
    y : int
}

[<Fact>]
let ``Reactive Extensions Generate Observable from time series`` ()=

     let mutable resultList = []

     let list = [{y = 1 
                  t = DateTimeOffset.Parse("01/01/2018 01:00")};
                 {y = 2 
                  t = DateTimeOffset.Parse("01/01/2018 01:01")};
                 {y = 3
                  t = DateTimeOffset.Parse("01/01/2018 01:02")};
                 {y = 4 
                  t = DateTimeOffset.Parse("01/01/2018 01:03")}] 
    
     let sched = Scheduler.Scheduler.Historical
     sched.AdvanceTo(DateTimeOffset.Parse("01/01/2018 00:59"))

     let obs = Observable.generateTimedOn sched list (fun x -> x <> [])
                                                     (fun x -> x.Tail)
                                                     (fun x -> x.Head)
                                                     (fun x -> x.Head.t )

     let subs = obs |> Observable.subscribeOn sched
                    |> Observable.timestampOn sched
                    |> Observable.subscribe (fun x -> resultList <- x::resultList)

     
     sched.AdvanceBy(TimeSpan.FromMinutes(1.0))
     Assert.Equal(1,resultList.Length)
     sched.AdvanceBy(TimeSpan.FromMinutes(1.0))
     Assert.Equal(2,resultList.Length)
     sched.AdvanceBy(TimeSpan.FromMinutes(1.0))
     Assert.Equal(3,resultList.Length)
     sched.AdvanceBy(TimeSpan.FromMinutes(1.0))
     Assert.Equal(4,resultList.Length)
     sched.AdvanceBy(TimeSpan.FromMinutes(30.0))
     Assert.Equal(4,resultList.Length)

     subs.Dispose()

[<Fact>]
let ``Reactive Extensions Sampling even no new data`` ()=

     let mutable resultList = []

     let list = [{y = 1 
                  t = DateTimeOffset.Parse("01/01/2018 01:00")};
                 {y = 2 
                  t = DateTimeOffset.Parse("01/01/2018 01:15")};
                 {y = 3
                  t = DateTimeOffset.Parse("01/01/2018 02:15")};
                 {y = 4 
                  t = DateTimeOffset.Parse("01/01/2018 02:30")}] 
    
     let sched = Scheduler.Scheduler.Historical
     sched.AdvanceTo(DateTimeOffset.Parse("01/01/2018 01:00"))

     let obs = Observable.generateTimedOn sched list (fun x -> x <> [])
                                                     (fun x -> x.Tail)
                                                     (fun x -> x.Head)
                                                     (fun x -> x.Head.t )

     let sam =
                Observable.intervalOn sched (TimeSpan.FromMinutes(15.0))
                    |> Observable.map (fun x-> {t = sched.Clock; y = 0})
      
               
     let subs = [sam;obs] |> Seq.ofList 
                          |> Observable.combineLatestSeqMap (fun (x) -> { t= (x|> (Seq.maxBy(fun y -> y.t))).t
                                                                          y=  x|> Seq.sumBy(fun y -> y.y)})  

                          |> Observable.throttleOn sched (TimeSpan.FromMilliseconds(1.0))
                          |> Observable.subscribeOn sched
                          |> Observable.subscribe (fun x -> resultList <- x::resultList)
                   

     
     sched.AdvanceBy(TimeSpan.FromMinutes(150.0))
     Assert.Equal(9,resultList.Length)

     subs.Dispose()


[<Fact>]
let ``Reactive Extensions Combine lastest from time series`` ()=

     let mutable resultList = []

     let list1 = [{y = 1
                   t = DateTimeOffset.Parse("01/01/2018 01:00")};
                  {y = 2 
                   t = DateTimeOffset.Parse("01/01/2018 01:02")};
                  {y = 3
                   t = DateTimeOffset.Parse("01/01/2018 01:04")};
                  {y = 4 
                   t = DateTimeOffset.Parse("01/01/2018 01:08")}] 

     
     let list2 = [{y = 5 
                   t = DateTimeOffset.Parse("01/01/2018 01:01")};
                  {y = 6 
                   t = DateTimeOffset.Parse("01/01/2018 01:03")};
                  {y = 7
                   t = DateTimeOffset.Parse("01/01/2018 01:05")};
                  {y = 8 
                   t = DateTimeOffset.Parse("01/01/2018 01:07")}] 


     let sched = Scheduler.Scheduler.Historical
     sched.AdvanceTo(DateTimeOffset.Parse("01/01/2018 00:59"))

     let ser1 = Observable.generateTimedOn sched list1 (fun x -> x <> [])
                                                       (fun x -> x.Tail)
                                                       (fun x -> x.Head)
                                                       (fun x -> x.Head.t )
                |> Observable.startWith [{t=DateTimeOffset.MinValue 
                                          y=0}]

     let ser2 = Observable.generateTimedOn sched list2 (fun x -> x <> [])
                                                       (fun x -> x.Tail)
                                                       (fun x -> x.Head)
                                                       (fun x -> x.Head.t )  
                |> Observable.startWith [{t=DateTimeOffset.MinValue 
                                          y=0}]

     let source = [ser1;ser2] |> Seq.ofList
                                                       
     let comb = source |> Observable.combineLatestSeqMap (fun x -> { t= (x|> (Seq.maxBy(fun y -> y.t))).t
                                                                     y=  x|> Seq.sumBy(fun y -> y.y)})  
                       |> Observable.subscribeOn sched
                       |> Observable.timestampOn sched
                       |> Observable.subscribe (fun x -> resultList <- x::resultList)
      
     sched.AdvanceBy(TimeSpan.FromMinutes(30.0))
     Assert.Equal(9,resultList.Length)
