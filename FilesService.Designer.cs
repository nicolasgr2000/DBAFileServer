
namespace DBAFileServer
{
    partial class FilesService
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
            this.filesTimer = new System.Timers.Timer();
            ((System.ComponentModel.ISupportInitialize)(this.filesTimer)).BeginInit();
            // 
            // filesTimer
            // 
            this.filesTimer.Enabled = true;
            this.filesTimer.Interval = 10800000D;
            this.filesTimer.Elapsed += new System.Timers.ElapsedEventHandler(this.filesTimer_Elapsed);
            // 
            // FilesService
            // 
            this.ServiceName = "FilesService";
            ((System.ComponentModel.ISupportInitialize)(this.filesTimer)).EndInit();

        }

        #endregion

        private System.Timers.Timer filesTimer;
    }
}
