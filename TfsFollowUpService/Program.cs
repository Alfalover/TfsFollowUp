using CredentialManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Configuration.Install;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;

namespace TfsFollowUpService
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] argv)
		{
			// Check if we are running as as Service
			if (!Environment.UserInteractive)
			{
				ServiceBase[] ServicesToRun;
				ServicesToRun = new ServiceBase[]
				{
					new TfsFollowUpService()
				};
				ServiceBase.Run(ServicesToRun);

			} else {
				// Running as console mode.
				Console.WriteLine("Toni TfsFollowUp Tool :");

				if (argv.Length > 0) { 
					switch (argv[0])
					{
						case "--credentials":
							if(argv.Length < 3)
								break;
							var user = argv[1];
							var pass = argv[2];

							var cred = new Credential() { Target = TfsFollowUpService.credentialTarget };
							cred.Password = pass;
							cred.Username = user;
							if (cred.Save())
								Console.WriteLine("Credentials stored succesfully.");
							else
								Console.WriteLine("Credentials not saved!");
							return;
						

						case "--install":
							ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
							Console.WriteLine("Service installed successfully");
							return;

						case "--uninstall":
							ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
							Console.WriteLine("Service uninstalled successfully");
							return;
					}
				}
				
				Console.WriteLine("Create credentials using : TfsFollowUpService.exe --credentials user pass");
				Console.WriteLine("Install as a service	    : TfsFollowUpService.exe --install");
				Console.WriteLine("Unsinstall as a service  : TfsFollowUpService.exe --uninstall");
			}
		}
	}

	[RunInstaller(true)]
	public class CustomServiceInstaller : Installer
	{
		private ServiceProcessInstaller process;
		private ServiceInstaller service;

		public CustomServiceInstaller()
		{
			process = new ServiceProcessInstaller();
			process.Account = ServiceAccount.User;
			service = new ServiceInstaller();
			service.DisplayName = TfsFollowUpService.DisplayName;
			service.Description = TfsFollowUpService.Description;
			service.StartType = ServiceStartMode.Automatic;
			Installers.Add(process);
			Installers.Add(service);
		}
	}
}
