using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;

namespace DBAFileServer
{
    /// <summary>
    /// Represents a Windows Service that handles file operations such as copying files from a source to a destination folder at regular intervals.
    /// </summary>
    /// <remarks>
    /// The <see cref="FilesService"/> class inherits from <see cref="ServiceBase"/> and is designed to perform automated file management tasks. The primary responsibilities of this service include:
    /// <list type="bullet">
    ///     <item>
    ///         <description>Starting and stopping a timer that triggers file copying operations at defined intervals.</description>
    ///     </item>
    ///     <item>
    ///         <description>Copying files from a source folder to a destination folder, including handling subdirectories and ensuring that files are not duplicated.</description>
    ///     </item>
    ///     <item>
    ///         <description>Logging information and errors related to file operations and service status to a specified log file.</description>
    ///     </item>
    ///     <item>
    ///         <description>Managing the log file size by rotating logs and maintaining a specified number of archived log files.</description>
    ///     </item>
    /// </list>
    /// <para>
    /// The service configuration parameters, such as the source and destination folders, timer interval, and log file path, are read from the application's configuration file (app.config). 
    /// The service also handles exceptions by logging errors to the Windows Event Log if file operations or logging encounters issues.
    /// </para>
    /// </remarks>
    partial class FilesService : ServiceBase
    {

        private bool isRunning  = false;
        private static readonly string logFilePath = ConfigurationManager.AppSettings["logFilePath"] + "\\service.dba";
        private const long maxLogFileSize = 256 * 1024 * 1024; // 512 MB        
        private const int maxLogFileCount = 4;


        /// <summary>
        /// Initializes a new instance of the <see cref="FilesService"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor sets up the service by initializing the components and configuring the timer.
        /// The timer interval is read from the application's configuration settings. If the interval is not valid,
        /// a default value is used, and an error is logged.
        /// </remarks>
        /// <exception cref="ConfigurationErrorsException">
        /// Thrown if the configuration settings cannot be read or parsed correctly. This exception is
        /// not thrown directly by the constructor but may be encountered if the settings are incorrect or
        /// the configuration file is not accessible.
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown if the timer interval setting cannot be parsed as a double. This exception is caught and
        /// handled internally by setting a default interval and logging an error.
        /// </exception>
        public FilesService()
        {
            InitializeComponent();
            double timerInterval;
            // Try to parse the timer interval from the configuration settings.
            if (double.TryParse(ConfigurationManager.AppSettings["timerInterval"], out timerInterval))
            {
                filesTimer.Interval = timerInterval;
            }
            else
            {
                // Log an error if the interval is not valid and set a default interval.
                EventLog.WriteEntry("DBAFileService", "Invalid timer interval in configuration.", EventLogEntryType.Error);
                filesTimer.Interval = 18000000; // Default to 5 hours
            }

            filesTimer.Elapsed += filesTimer_Elapsed;
        }

        /// <summary>
        /// Starts the service and begins the file copying process.
        /// </summary>
        /// <param name="args">An array of command-line arguments passed to the service. This parameter is not used in this implementation.</param>
        /// <remarks>
        /// This method is called when the service is started. It logs a message indicating that the service is starting,
        /// and then starts the timer that controls the file copying process. The timer will trigger the file copying
        /// operation at intervals specified by the timer's interval property.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is an issue starting the timer. This exception is not explicitly handled here but can
        /// occur if the timer's state is invalid.
        /// </exception>
        protected override void OnStart(string[] args)
        {
            Log("Starting DBAFileService service.");
            filesTimer.Start();
        }

        /// <summary>
        /// Stops the service and halts the file copying process.
        /// </summary>
        /// <remarks>
        /// This method is called when the service is stopped. It sets a flag to indicate that the service is no longer running,
        /// stops the timer that controls the file copying process, and logs a message indicating that the service is stopping.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is an issue stopping the timer. This exception is not explicitly handled here but can
        /// occur if the timer's state is invalid or if the timer is already stopped.
        /// </exception>
        protected override void OnStop()
        {
            isRunning  = false;
            filesTimer.Stop();
            Log("Stopping DBAFileService service.");
        }

        /// <summary>
        /// Handles the Elapsed event of the timer. This method is called at each interval specified by the timer.
        /// </summary>
        /// <param name="sender">The source of the event. This is typically the timer instance that triggered the event.</param>
        /// <param name="e">An instance of <see cref="System.Timers.ElapsedEventArgs"/> containing event data.</param>
        /// <remarks>
        /// This method executes the file copy process at each timer interval. It performs the following actions:
        /// <list type="bullet">
        ///     <item>
        ///         <description>Checks if the file copy process is currently running by examining the <see cref="isRunning"/> flag.</description>
        ///     </item>
        ///     <item>
        ///         <description>If not running, it sets the <see cref="isRunning"/> flag to true to prevent overlapping executions.</description>
        ///     </item>
        ///     <item>
        ///         <description>Logs the start of the file copy process with a timestamp.</description>
        ///     </item>
        ///     <item>
        ///         <description>Retrieves the source and destination folder paths from the application configuration.</description>
        ///     </item>
        ///     <item>
        ///         <description>Calls the <see cref="CopyFiles"/> method to perform the file copying operation.</description>
        ///     </item>
        ///     <item>
        ///         <description>Logs the conclusion of the file copy process.</description>
        ///     </item>
        ///     <item>
        ///         <description>Logs any exceptions that occur during the file copy process.</description>
        ///     </item>
        ///     <item>
        ///         <description>Resets the <see cref="isRunning"/> flag to false in the <see cref="finally"/> block to ensure that the flag is properly reset.</description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <exception cref="ConfigurationErrorsException">
        /// Thrown if the configuration settings cannot be read. This exception is not handled in this method but could occur if the
        /// configuration file is missing or improperly formatted.
        /// </exception>
        /// <exception cref="IOException">
        /// Thrown if there is an issue with file operations during the copy process. This exception is caught and logged.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown if there is a permission issue accessing the files or directories. This exception is caught and logged.
        /// </exception>
        private void filesTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (isRunning) return;
            isRunning = true;
            try
            {
                Log($"Executing file copy process at {DateTime.Now}");
                string sourceFolder = ConfigurationManager.AppSettings["sourceFolder"];
                string destinationFolder = ConfigurationManager.AppSettings["destinationFolder"];
                CopyFiles(sourceFolder, destinationFolder);
                Log("Concluded the process of copying backup files to the server.");
            }
            catch (Exception ex)
            {
                Log($"Error during file copy process: {ex.Message}");
            }
            finally
            {
                isRunning = false;
            }
        }

        /// <summary>
        /// Copies files from the source folder to the destination folder, including subdirectories and their contents.
        /// </summary>
        /// <param name="sourceFolder">The path to the source folder from which files will be copied. This folder must exist for the operation to proceed.</param>
        /// <param name="destinationFolder">The path to the destination folder where files will be copied to. Subdirectories will also be created as needed.</param>
        /// <remarks>
        /// This method performs the following actions:
        /// <list type="bullet">
        ///     <item>
        ///         <description>Checks if the source folder exists. If it does not, a log entry is created and the method returns without performing any operations.</description>
        ///     </item>
        ///     <item>
        ///         <description>Copies all files with a ".bak" extension from the source folder to the destination folder. Each file is copied using the <see cref="CopyFile"/> method.</description>
        ///     </item>
        ///     <item>
        ///         <description>Creates any necessary subdirectories in the destination folder that correspond to the subdirectories in the source folder. This is done using <see cref="Directory.CreateDirectory"/>.</description>
        ///     </item>
        ///     <item>
        ///         <description>Recursively copies the contents of each subdirectory from the source to the destination using the <see cref="CopyDirectory"/> method.</description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <exception cref="IOException">
        /// Thrown if an I/O error occurs while accessing files or directories. This exception is caught and logged in the <see cref="catch"/> block.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown if there are permission issues when accessing files or directories. This exception is caught and logged in the <see cref="catch"/> block.
        /// </exception>
        private void CopyFiles(string sourceFolder, string destinationFolder)
        {
            if (!Directory.Exists(sourceFolder))
            {
                Log($"The source folder '{sourceFolder}' does not exist.");
                return;
            }

            try
            {
                foreach (string filePath in Directory.GetFiles(sourceFolder, "*.bak"))
                {
                    string destFilePath = Path.Combine(destinationFolder, Path.GetFileName(filePath));
                    CopyFile(filePath, destFilePath);
                }

                foreach (string subDir in Directory.GetDirectories(sourceFolder))
                {
                    string destSubDir = Path.Combine(destinationFolder, Path.GetFileName(subDir));
                    if (!Directory.Exists(destSubDir))
                    {
                        Directory.CreateDirectory(destSubDir);
                    }
                    CopyDirectory(subDir, destSubDir);
                }
            }
            catch (Exception ex)
            {
                Log($"Error copying files: {ex.Message}");
            }
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            try
            {
                // Copy all files in the directory
                foreach (string filePath in Directory.GetFiles(sourceDir, "*.bak"))
                {
                    string destFilePath = Path.Combine(destDir, Path.GetFileName(filePath));
                    CopyFile(filePath, destFilePath);
                }

                // Copy all subdirectories
                foreach (string subDir in Directory.GetDirectories(sourceDir))
                {
                    string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                    if (!Directory.Exists(destSubDir))
                    {
                        Directory.CreateDirectory(destSubDir);
                    }
                    CopyDirectory(subDir, destSubDir);
                }
            }
            catch (Exception ex)
            {
                Log($"Error copying directory '{sourceDir}' to '{destDir}': {ex.Message}");
            }
        }

        /// <summary>
        /// Recursively copies files and subdirectories from the source directory to the destination directory.
        /// </summary>
        /// <param name="sourceDir">The path to the source directory from which files and subdirectories will be copied. This directory must exist for the operation to proceed.</param>
        /// <param name="destDir">The path to the destination directory where files and subdirectories will be copied to. Subdirectories will be created as needed.</param>
        /// <remarks>
        /// This method performs the following actions:
        /// <list type="bullet">
        ///     <item>
        ///         <description>Copies all files with a ".bak" extension from the source directory to the destination directory. Each file is copied using the <see cref="CopyFile"/> method.</description>
        ///     </item>
        ///     <item>
        ///         <description>Creates any necessary subdirectories in the destination directory that correspond to the subdirectories in the source directory. This is done using <see cref="Directory.CreateDirectory"/>.</description>
        ///     </item>
        ///     <item>
        ///         <description>Recursively copies the contents of each subdirectory from the source to the destination using a recursive call to <see cref="CopyDirectory"/>.</description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <exception cref="IOException">
        /// Thrown if an I/O error occurs while accessing files or directories. This exception is caught and logged in the <see cref="catch"/> block.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown if there are permission issues when accessing files or directories. This exception is caught and logged in the <see cref="catch"/> block.
        /// </exception>
        private void CopyFile(string sourceFile, string destinationFilePath)
        {

            try
            {
                string destinationDirectory = Path.GetDirectoryName(destinationFilePath);

                if (!Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                if (!File.Exists(destinationFilePath))
                {
                    File.Copy(sourceFile, destinationFilePath);
                    Log($"Copied file '{sourceFile}' to '{destinationFilePath}'.");
                }
                else
                {
                    Log($"File '{destinationFilePath}' already exists. Skipping copy.");
                }
            }
            catch (Exception ex)
            {
                Log($"Error copying file '{sourceFile}' to '{destinationFilePath}': {ex.Message}");
            }
        }

        /// <summary>
        /// Writes a log message to the log file. If the log file exceeds the maximum size, it rotates the log files before writing the new message.
        /// </summary>
        /// <param name="message">The message to be logged. This should contain information relevant to the log entry.</param>
        /// <remarks>
        /// This method performs the following actions:
        /// <list type="bullet">
        ///     <item>
        ///         <description>Checks if the log file exists and whether its size exceeds the maximum allowed size defined by <see cref="maxLogFileSize"/>.</description>
        ///     </item>
        ///     <item>
        ///         <description>If the log file size exceeds the maximum limit, it invokes the <see cref="RotateLogFiles"/> method to manage old log files.</description>
        ///     </item>
        ///     <item>
        ///         <description>Appends the log message to the log file, including a timestamp. The log message is written using <see cref="StreamWriter"/> in append mode.</description>
        ///     </item>
        ///     <item>
        ///         <description>Handles any exceptions that occur during the file write operation by logging an error entry in the Windows Event Log.</description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <exception cref="Exception">
        /// Thrown if an error occurs while accessing or writing to the log file. The exception is caught and logged to the Windows Event Log.
        /// </exception>
        private static void Log(string message)
        {
            try
            {
                FileInfo logFileInfo = new FileInfo(logFilePath);

                if (logFileInfo.Exists && logFileInfo.Length > maxLogFileSize)
                {
                    RotateLogFiles();
                }

                using (StreamWriter sw = new StreamWriter(logFilePath, true))
                {
                    sw.WriteLine($"{DateTime.Now}: {message}");
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("DBAFileService", $"Failed to write to log file: {ex.Message}", EventLogEntryType.Error);
            }
        }

        /// <summary>
        /// Archives the current log file by renaming it with a timestamp, and manages the number of archived log files to ensure only a specified number of logs are retained.
        /// </summary>
        /// <remarks>
        /// This method performs the following actions:
        /// <list type="bullet">
        ///     <item>
        ///         <description>Renames the current log file by appending a timestamp to its name. This creates a unique filename based on the current date and time.</description>
        ///     </item>
        ///     <item>
        ///         <description>Calls the <see cref="ManageArchivedLogs"/> method to handle the retention and deletion of archived log files, ensuring that only a defined number of logs are kept.</description>
        ///     </item>
        ///     <item>
        ///         <description>If any error occurs during the archival process, such as issues with file access or renaming, it is caught and logged as an error in the Windows Event Log.</description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <exception cref="Exception">
        /// Thrown if an error occurs while renaming the log file or managing archived logs. The exception is caught and logged to the Windows Event Log.
        /// </exception>
        private static void RotateLogFiles()
        {
            try
            {
                // Archivar el archivo actual
                string archiveLogFilePath = logFilePath + "." + DateTime.Now.ToString("yyyyMMddHHmmss");
                File.Move(logFilePath, archiveLogFilePath);

                // Gestionar el número de archivos archivados
                ManageArchivedLogs();
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("DBAFileService", $"Failed to rotate log file: {ex.Message}", EventLogEntryType.Error);
            }
        }

        /// <summary>
        /// Manages archived log files by deleting older log files when the number of archived logs exceeds a specified limit.
        /// </summary>
        /// <remarks>
        /// This method performs the following actions:
        /// <list type="bullet">
        ///     <item>
        ///         <description>Retrieves the directory containing the log files using the directory of the current log file path.</description>
        ///     </item>
        ///     <item>
        ///         <description>Filters the files in the directory to include only those with names matching the pattern "service.dba.*". These files are then ordered by their creation time.</description>
        ///     </item>
        ///     <item>
        ///         <description>Checks if the number of archived log files meets or exceeds the specified maximum number of allowed log files, defined by <see cref="maxLogFileCount"/>.</description>
        ///     </item>
        ///     <item>
        ///         <description>If the number of log files exceeds the maximum allowed, deletes the oldest files to ensure that only the most recent log files are retained.</description>
        ///     </item>
        ///     <item>
        ///         <description>If an error occurs during the management of log files, such as issues with file access or deletion, it is caught and logged as an error in the Windows Event Log.</description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <exception cref="Exception">
        /// Thrown if an error occurs while retrieving or deleting log files. The exception is caught and logged to the Windows Event Log.
        /// </exception>
        private static void ManageArchivedLogs()
        {
            try
            {
                DirectoryInfo logDirectory = new FileInfo(logFilePath).Directory;
                if (logDirectory != null)
                {
                    FileInfo[] logFiles = logDirectory.GetFiles("service.dba.*")
                                                     .OrderBy(f => f.CreationTime)
                                                     .ToArray();

                    if (logFiles.Length >= maxLogFileCount)
                    {
                        for (int i = 0; i < logFiles.Length - maxLogFileCount + 1; i++)
                        {
                            logFiles[i].Delete();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("DBAFileService", $"Failed to manage archived log files: {ex.Message}", EventLogEntryType.Error);
            }
        }
    }
}
