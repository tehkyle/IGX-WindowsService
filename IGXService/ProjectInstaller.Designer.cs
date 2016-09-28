namespace IGXNavService
{
	partial class ProjectInstaller
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.IgxServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
			this.IgxServiceInstaller = new System.ServiceProcess.ServiceInstaller();
			// 
			// IgxServiceProcessInstaller
			// 
			this.IgxServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
			this.IgxServiceProcessInstaller.Password = null;
			this.IgxServiceProcessInstaller.Username = null;
			this.IgxServiceProcessInstaller.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.IgxServiceProcessInstaller_AfterInstall);
			// 
			// IgxServiceInstaller
			// 
			this.IgxServiceInstaller.Description = "Monitors a SQL Database and syncs changes";
			this.IgxServiceInstaller.DisplayName = "Ingeniux API Service";
			this.IgxServiceInstaller.ServiceName = "IGXWindowsService";
			this.IgxServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
			this.IgxServiceInstaller.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.IgxServiceInstaller_AfterInstall);
			// 
			// ProjectInstaller
			// 
			this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.IgxServiceProcessInstaller,
            this.IgxServiceInstaller});

		}

		#endregion

		private System.ServiceProcess.ServiceProcessInstaller IgxServiceProcessInstaller;
		private System.ServiceProcess.ServiceInstaller IgxServiceInstaller;
	}
}