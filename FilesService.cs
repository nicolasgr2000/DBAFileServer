using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Text;

namespace DBAFileServer
{
    partial class FilesService : ServiceBase
    {

        private bool flat = false;

        public FilesService()
        {            
            InitializeComponent();

        }

        protected override void OnStart(string[] args)
        {
            filesTimer.Start();
        }

        protected override void OnStop()
        {
            flat = false;
            filesTimer.Stop();
        }

        private void filesTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (flat) return;
            try
            {
                flat = true;
                EventLog.WriteEntry($"Starting DBAFileService service at {DateTime.Now}", EventLogEntryType.Information);
                string sourceFolder = ConfigurationManager.AppSettings["sourceFolder"].ToString();
                string destinationFolder = ConfigurationManager.AppSettings["destinationFolder"].ToString();
                CopyFiles(sourceFolder, destinationFolder);
                EventLog.WriteEntry("Concluded the process of copying backup files to the server.", EventLogEntryType.Information);            
            } catch (Exception ex)
            {
                EventLog.WriteEntry(ex.Message, EventLogEntryType.Error);
            } finally
            {
                flat = false;
            }
        }

        private void CopyFiles(string sourceFolder, string destinationFolder)
        {
            try
            {
                if (!Directory.Exists(sourceFolder))
                {
                    EventLog.WriteEntry($"The source folder '{sourceFolder}' does not exist.", EventLogEntryType.Error);                    
                    return;
                }

                // Copy files in the root of sourceFolder
                string[] rootFiles = Directory.GetFiles(sourceFolder, "*.bak");
                foreach (string rootFile in rootFiles)
                {
                    string fileName = Path.GetFileName(rootFile);
                    string destinationFilePath = Path.Combine(destinationFolder, fileName);

                    if (!File.Exists(destinationFilePath))
                    {
                        EventLog.WriteEntry($"Copied file '{rootFile}' to '{destinationFilePath}'.", EventLogEntryType.Information);                    
                        CopyFile(rootFile, destinationFilePath);
                    }
                    else
                    {
                        EventLog.WriteEntry($"File '{destinationFilePath}' already exists. Skipping copy.", EventLogEntryType.Information);
                    }
                }

                // Copy subdirectories and their files
                string[] subDirectories = Directory.GetDirectories(sourceFolder);
                foreach (string subDir in subDirectories)
                {
                    string dirName = Path.GetFileName(subDir);
                    string destSubDir = Path.Combine(destinationFolder, dirName);

                    if (!Directory.Exists(destSubDir))
                    {
                        Directory.CreateDirectory(destSubDir);
                    }

                    CopyDirectory(subDir, destSubDir);
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(ex.Message, EventLogEntryType.Error);
            }
            finally
            {
                flat = false;
            }
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            // Copy all files
            string[] files = Directory.GetFiles(sourceDir, "*.bak");
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destDir, fileName);

                if (!File.Exists(destFile))
                {
                    EventLog.WriteEntry("Copy " + fileName, EventLogEntryType.Information);
                    CopyFile(file, destFile);
                }
                else
                {
                    EventLog.WriteEntry("File " + fileName + " already exists. Skipping copy.", EventLogEntryType.Information);
                }
            }

            // Copy all subdirectories
            string[] subDirs = Directory.GetDirectories(sourceDir);
            foreach (string subDir in subDirs)
            {
                string dirName = Path.GetFileName(subDir);
                string destSubDir = Path.Combine(destDir, dirName);

                if (!Directory.Exists(destSubDir))
                {
                    Directory.CreateDirectory(destSubDir);
                }

                CopyDirectory(subDir, destSubDir);
            }
        }

        private void CopyFile(string sourceFile, string destinationFilePath)
        {
            try
            {
                File.Copy(sourceFile, destinationFilePath);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(ex.Message, EventLogEntryType.Error);
            }
        }


        private string GetRelativePath(string fromPath, string toPath)
        {
            Uri fromUri = new Uri(fromPath);
            Uri toUri = new Uri(toPath);

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
