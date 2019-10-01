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
open System.Reflection
open Microsoft.AspNetCore.Server.Kestrel.Core

type Startup(env: IHostingEnvironment) =
 
    member this.ConfigureServices(services: IServiceCollection) =
       
        
        services.Configure<KestrelServerOptions>( fun (ko:KestrelServerOptions) -> ko.AllowSynchronousIO <- true
                                                                                   () )
                .AddSingleton<SessionService,SessionService>()
                .AddSingleton<TfsService,TfsService>()
                .AddSingleton<UpdateService,UpdateService>()
                .AddTransient<CapacityService,CapacityService>()
                .AddTransient<WorkItemService,WorkItemService>()
                .AddTransient<TeamService,TeamService>()
                .AddLogging()
                .AddMvcCore(fun x -> x.EnableEndpointRouting <- false)
                .AddJsonOptions(fun x -> x.JsonSerializerOptions.PropertyNamingPolicy <- null)
                |> ignore

     member this.Configure (app: IApplicationBuilder) =
        app.UseDeveloperExceptionPage()
           .UseMvc()
           .UseFileServer(false)
           |> ignore

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

let GetExecutingDirectoryName() = 

    let location = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase)
    let fileInfo = (new FileInfo(location.AbsolutePath))
    fileInfo.Directory.FullName
    |> Uri.UnescapeDataString

let TfsFollowUpInitialize (argv:string[]) =

            printfn "Tfs Toni's follow up Tooling... rev 5"

            // Web server
            let execPath = GetExecutingDirectoryName()
            let contentRoot = Path.Combine(execPath,"webroot")
            let hostAddress = if argv.Length > 0 then argv.[0] else "http://0.0.0.0:4321"

            printfn "%s" hostAddress

            let cfgFiles = Directory.EnumerateFiles("/run/secrets/")
                            |> Seq.filter(fun file -> file.Contains("tfsfollowup"))
                            |> Seq.tryHead

            let configure =
                 cfgFiles
                 |> function
                    | Some file -> 
                         printfn "--> using docker secret... %s" file
                         new Action<IConfigurationBuilder> (
                             fun x -> x.AddJsonFile(file,false,true)  |> ignore
                                      )
                    | None -> 
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
