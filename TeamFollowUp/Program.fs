// Learn more about F# at http://fsharp.org

open System
open System.IO
open System.Security
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Builder
open System.Threading
open Tfs
open Update
open Session
open Capacity
open WorkItem
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Configuration.Json


type Startup(env: IHostingEnvironment) =
 
    member this.ConfigureServices(services: IServiceCollection) =
       
       
        services.AddSingleton<SessionService,SessionService>()
                .AddSingleton<TfsService,TfsService>()
                .AddSingleton<UpdateService,UpdateService>()
                .AddTransient<CapacityService,CapacityService>()
                .AddTransient<WorkItemService,WorkItemService>()
                .AddMvcCore() |> ignore

     member this.Configure (app: IApplicationBuilder) =

        app.UseMvc().UseFileServer(false) |> ignore

[<EntryPoint>]
let main argv  =
    
    printfn "Tfs Toni's follow up Tooling..."

    // Web server
    let contentRoot = Path.Combine(Directory.GetCurrentDirectory(),"webroot")
    let hostAddress = if argv.Length > 0 then argv.[0] else "http://+:4321"

    let configure =
        new Action<IConfigurationBuilder> (
            fun x -> x.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(),"appsettings.json"),false,true)  |> ignore)

    let host = WebHostBuilder()
                    .UseUrls(hostAddress)
                    .ConfigureAppConfiguration(configure)
                    .UseWebRoot(contentRoot)
                    .UseContentRoot(contentRoot)
                    .UseKestrel()
                    .UseStartup<Startup>().Build()


    let uService = host.Services.GetService<UpdateService>()
    let sService = host.Services.GetService<SessionService>()
   
    let session = sService.createSession

    // Start tfs data refresher
    Async.Start (uService.periodicUpdate session)
   

    host.RunAsync() |> ignore   

    // Wait until there is valid data loaded
    while  (let x = uService.SprintsList
            x.IsEmpty)  do
        Thread.Sleep(100)

    printfn "Data ready..."

    Console.ReadLine() |> ignore
    0

