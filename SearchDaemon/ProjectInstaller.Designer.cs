namespace SearchDaemon
{
	partial class ProjectInstaller
	{
		/// <summary>
		/// Обязательная переменная конструктора.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Освободить все используемые ресурсы.
		/// </summary>
		/// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Код, автоматически созданный конструктором компонентов

		/// <summary>
		/// Требуемый метод для поддержки конструктора — не изменяйте 
		/// содержимое этого метода с помощью редактора кода.
		/// </summary>
		private void InitializeComponent()
		{
			this.searchDaemonProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
			this.searchDaemonInstaller = new System.ServiceProcess.ServiceInstaller();
			// 
			// searchDaemonProcessInstaller
			// 
			this.searchDaemonProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
			this.searchDaemonProcessInstaller.Password = null;
			this.searchDaemonProcessInstaller.Username = null;
			// 
			// searchDaemonInstaller
			// 
			this.searchDaemonInstaller.Description = "Сервис поиска файлов";
			this.searchDaemonInstaller.DisplayName = "SearchDaemon";
			this.searchDaemonInstaller.ServiceName = "SearchDaemon";
			this.searchDaemonInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
			// 
			// ProjectInstaller
			// 
			this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.searchDaemonProcessInstaller,
            this.searchDaemonInstaller});

		}

		#endregion

		private System.ServiceProcess.ServiceProcessInstaller searchDaemonProcessInstaller;
		private System.ServiceProcess.ServiceInstaller searchDaemonInstaller;
	}
}