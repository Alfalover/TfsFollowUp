module Session

open System.Security
open System.Net
open Microsoft.Extensions.Configuration
open System

    type sessionHolder = { credentials:ICredentials }

    type SessionService(config : IConfiguration) =
        

        let GetPassword (): SecureString =
                let mutable continueLooping = true
                let pwd = new SecureString()
                while continueLooping do
                    let i = Console.ReadKey(true)
                    match i.Key with 
                        | ConsoleKey.Enter -> continueLooping <- false
                        | ConsoleKey.Backspace -> if pwd.Length > 0 then pwd.RemoveAt(pwd.Length-1);  Console.Write("\b \b")
                        | _ -> pwd.AppendChar(i.KeyChar); Console.Write("*")
                Console.WriteLine();
                pwd

        let GetCredential user (pass:SecureString) :ICredentials =   
            let domain = config.Item "credentials:domain"
            new NetworkCredential(user,pass,domain) :> ICredentials  

        member val currentSession : sessionHolder = {credentials = (new NetworkCredential("","","") :> ICredentials)} with get,set    
                                                                          
        member this.createSession =   
         
            // Crendentials
            printfn "Enter user:"
            let user = if String.IsNullOrWhiteSpace(config.Item "credentials:user") then
                          printfn "Enter user:"
                          Console.ReadLine()
                       else 
                          printfn "user:"
                          Console.WriteLine(config.Item "credentials:user")
                          config.Item "credentials:user"

            printfn "Enter password:"
            let pass = GetPassword() 

            let session = { credentials= GetCredential user pass }       
            this.currentSession <- session
            session
