namespace aspnetcore3.Controllers

open Microsoft.AspNetCore.Mvc
open Update
open Newtonsoft.Json
open Tfs
open WorkItem
open System
open FSharp.Control.Reactive
open TimeSeries
open System.IO

type WorkSummary = {

    sprint : Sprint
    summary : wGlobalStats
    lastUpdate : DateTime
}

type GroupRequest = {
    dateFrom:DateTimeOffset 
    dateTo:DateTimeOffset 
    globid:string 
    globName: string
    id:int list 
    field:string
}

type serieDebugInfo = {
    request : GroupRequest
    sourceSeries : datapoint list list
    resultSerie : datapoint list
}

type serieProjection = {
    pbiId:  string 
    name : string
    projectedField : string
    count: int 
    serie: datapoint[]
}

[<Route("api/WorkFollow")>]
type WorkFollowController(workitems : WorkItemService, update : UpdateService) =
    inherit ControllerBase()
    
    member private this.get<'a> (prop : string) (object : 'a) : 'Result option =
            try
              let p = object.GetType().GetProperty(prop)
              Some (p.GetValue(object, null) :?> 'Result)
            with ex -> printfn "%s" ex.Message; None

    member private this.getSerie field wIL =  
                       wIL |> List.map(fun x -> { t =  DateTimeOffset.Parse (x.fields.``System.ChangedDate``) 
                                                  y = this.get field x.fields 
                                                        |> Option.defaultValue 0.0 
                                                        })    

    [<Route("Summary")>]
    member this.Summary(wtype:string) = 
                
        update.SprintsList  |> List.tryFind(fun x -> x.attributes.timeFrame = TimeFrame.current)
                            |> function 
                                | Some current -> { sprint = current
                                                    summary = current |> workitems.GetGlobalWorkStats wtype
                                                    lastUpdate = update.LastUpdate}
                                                  |> JsonConvert.SerializeObject
                                | None -> ""

    [<Route("Projection")>]
    member this.Projection (id:string) (field:string) =         
        let data =  update.GetRevisions (int id)
        let serie = data.value 
                        |> List.ofArray
                        |> this.getSerie field 

 
        { pbiId = id
          name = "Projection"
          projectedField = field
          count = serie.Length
          serie = serie |> Array.ofList  }
        |> JsonConvert.SerializeObject
    
 
    [<HttpPost>]
    [<Route("GroupProjection")>]
    member this.GroupProjection ([<FromBody>]request:GroupRequest) =
                                
        let data = request.id |> List.map(fun x -> update.GetRevisions x)
        
        let series = data |> List.map(fun x -> x.value |> List.ofArray 
                                                      |> this.getSerie request.field )  


        let serie = series 
                         |> sumSeries request.dateFrom request.dateTo (TimeSpan.FromHours(24.0))
                         |> cutStartSerie request.dateFrom
                         |> extendEndSerie request.dateTo
                         |> cutEndSerie request.dateTo
                         |> removeWeekends

        let debug = {request= request
                     sourceSeries= series
                     resultSerie = serie
                    }
                    |> JsonConvert.SerializeObject
       
       //let dname = 
        //    sprintf ".\projlog\GroupProjection %s-%s" request.globid (DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"))

        //File.WriteAllText (dname,debug) ;

        
        { pbiId = request.globid
          name = request.globName
          projectedField = request.field
          count = serie.Length
          serie = serie |> Array.ofList  
          }
        |> JsonConvert.SerializeObject


    [<HttpGet>]
    member this.Get(wtype:string) = 
        update.SprintsList  
        |> List.filter(fun x -> x.attributes.timeFrame = TimeFrame.current)
        |> List.head
        |>  workitems.GetWorkItemStats wtype
        |> JsonConvert.SerializeObject

    [<Route("GetAll")>]
    member this.GetAll() = 
        update.SprintsList  
        |> List.filter(fun x -> x.attributes.timeFrame = TimeFrame.current)
        |> List.head
        |>  workitems.GetAllStats
        |> JsonConvert.SerializeObject