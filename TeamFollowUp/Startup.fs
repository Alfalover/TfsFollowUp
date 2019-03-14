module TfsFollowUp

open System
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Session
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Builder
open Tfs
open Update
open Capacity
open WorkItem
open Microsoft.AspNetCore.Diagnostics
open Microsoft.AspNetCore.Mvc
open System.IO
open System.Threading
open TeamFollow

type Startup(env: IHostingEnvironment) =
 
    member this.ConfigureServices(services: IServiceCollection) =
       
        
        services.AddSingleton<SessionService,SessionService>()
                .AddSingleton<TfsService,TfsService>()
                .AddSingleton<UpdateService,UpdateService>()
                .AddTransient<CapacityService,CapacityService>()
                .AddTransient<WorkItemService,WorkItemService>()
                .AddTransient<TeamService,TeamService>()
                .AddLogging()
                .AddMvcCore()
                .AddJsonFormatters()
                |> ignore

     member this.Configure (app: IApplicationBuilder) =
        app.UseDeveloperExceptionPage().UseMvc().UseFileServer(false) |> ignore

let TfsFollowUpStart(host:IWebHost) (session) =   
            // Start tfs data refresher
            let uService = host.Services.GetService<UpdateService>()
            Async.Start (uService.periodicUpdate session)
   

            host.RunAsync() |> ignore   

            // Wait until there is valid data loaded
            while  (let x = uService.SprintsList
                    x.IsEmpty)  do
                            Thread.Sleep(100)

            printfn "Data ready..."

let TfsFollowUpInitialize (argv:string[]) =

            printfn "Tfs Toni's follow up Tooling... rev 4"

            // Web server
            let contentRoot = Path.Combine(Directory.GetCurrentDirectory(),"webroot")
            let hostAddress = if argv.Length > 0 then argv.[0] else "http://+:4321"

            let configure =
                File.Exists("/run/secrets/tfsfollowup")
                 |> function
                    | true -> 
                         printfn "--> using docker secret..."
                         new Action<IConfigurationBuilder> (
                             fun x -> x.AddJsonFile("/run/secrets/tfsfollowup",false,true)  |> ignore
                                      )
                    | false -> 
                         printfn "--> using local appsettings.json.."
                         new Action<IConfigurationBuilder> (
                             fun x -> x.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(),"appsettings.json"),false,true)  |> ignore
                                      )

            let host = WebHostBuilder()
                            .UseUrls(hostAddress)
                            .UseWebRoot(contentRoot)
                            .UseContentRoot(contentRoot)
                            .UseKestrel()
                            .ConfigureAppConfiguration(configure)
                            .UseStartup<Startup>().Build()


            host
