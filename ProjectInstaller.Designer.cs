
namespace DBAFileServer
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de componentes

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.DBAFileServerProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.DBAFilerServerServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // DBAFileServerProcessInstaller
            // 
            this.DBAFileServerProcessInstaller.Account = System.ServiceProcess.ServiceAccount.NetworkService;
            this.DBAFileServerProcessInstaller.Password = null;
            this.DBAFileServerProcessInstaller.Username = null;
            // 
            // DBAFilerServerServiceInstaller
            // 
            this.DBAFilerServerServiceInstaller.Description = "Server to copy backups files into external location.";
            this.DBAFilerServerServiceInstaller.DisplayName = "DBAFileServer";
            this.DBAFilerServerServiceInstaller.ServiceName = "DBAFileService";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.DBAFileServerProcessInstaller,
            this.DBAFilerServerServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller DBAFileServerProcessInstaller;
        private System.ServiceProcess.ServiceInstaller DBAFilerServerServiceInstaller;
    }
}