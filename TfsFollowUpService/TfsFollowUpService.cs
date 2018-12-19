using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Tfs;
using CredentialManagement;
using static Session;

namespace TfsFollowUpService
{
	public partial class TfsFollowUpService : ServiceBase
	{
		public static string credentialTarget = "TfsFollowUpCredential";
		public static string DisplayName = "Tfs Follow Up";
		public static string Description =" Toni TFS Follow Up Tool";

		private IWebHost host;

		public TfsFollowUpService()
		{
			InitializeComponent();
			this.ServiceName= DisplayName;
		}

		protected override void OnStart(string[] args)
		{
			this.host = TfsFollowUp.TfsFollowUpInitialize(args);

			var credential = new Credential() { Target="TfsFollowUpCredential"};

			if (credential.Load()) { 

				// Resolve 
				SessionService sService = (SessionService)this.host.Services.GetService(typeof(SessionService));

				var session =  sService.createSessionFromInputs(credential.Username,credential.SecurePassword);

				TfsFollowUp.TfsFollowUpStart(host,session);

			} else
			{
				throw new ArgumentNullException("Missing credentials! call /createCredential user pass from command prompt");
			}
		}

		protected override void OnStop()
		{
			this.host.Dispose();
		}
	}
}
