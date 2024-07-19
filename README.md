# DBAFileServer

DBAFileServer is a Windows Service designed to copy backup files (.bak) from a specified source directory to a destination directory. This service periodically checks the source directory and replicates its structure and contents to the destination directory, omitting any files that already exist in the destination.
Features

Automated Backup File Copying: Copies all .bak files from the source to the destination directory while maintaining the directory structure.
Configurable Source and Destination Paths: The service reads the source and destination paths from a configuration file.
Logging: Logs all activities, including start and stop events, file copying actions, and any errors encountered, to the Windows Event Log.
Scheduled Execution: Runs every 3 hours by default.

##Features

  * Automated Backup File Copying: Copies all .bak files from the source to the destination directory while maintaining the directory structure.
  * Configurable Source and Destination Paths: The service reads the source and destination paths from a configuration file.
  * Logging: Logs all activities, including start and stop events, file copying actions, and any errors encountered, to a specified log file.
  * Scheduled Execution: Runs every 5 hours by default (configurable).
  * Log Management: Implements log file rotation and archiving to manage log file size and retention.

## Configuration
### App.config

The App.config file should contain the following keys to specify the source and destination directories:
```xml
    <configuration>
      <appSettings>
        <add key="sourceFolder" value="E:\Test"/>
        <add key="destinationFolder" value="E:\TestDestination"/>
        <add key="logFilePath" value="E:\Temp\Log"/>
	    <add key="timerInterval" value="18000000"/>
      </appSettings>
    </configuration>    
```
### Key Configurations

* sourceFolder: Path to the source directory containing the .bak files.
* destinationFolder: Path to the destination directory where the .bak files will be copied.
* logFilePath: Path to the log file where service activities will be recorded.
* timerInterval: Interval in milliseconds between each execution of the file copying process (default is 5 hours or 18,000,000 milliseconds).


## Code Summary
### Service Initialization

The service initializes the filesTimer with a 3-hour interval (10,800,000 milliseconds) and starts the timer when the service starts.
Timer Elapsed Event

When the timer elapses, the service:

  * Logs the start of the copying process.
  * Reads the source and destination paths from the configuration.
  * Copies the files and directories from the source to the destination.
  * Logs the completion of the process or any errors encountered.

### Copying Logic

The service:

  * Checks if the source directory exists.
  * Copies all .bak files in the root of the source directory to the destination directory.
  * Recursively copies all subdirectories and their .bak files to the destination, maintaining the directory structure.
  * Skips copying files that already exist in the destination.

### Event Logging

The service logs events using the Windows Event Log, providing information on:

  * Service start and stop events.
  * Directory creation.
  * File copying, including paths and skip notifications if the file already exists.
  * Errors encountered during the process.

### Log Management

The service logs events to a specified log file, including:
    * Service start and stop events.
    * Directory creation.
    * File copying, including paths and skip notifications if the file already exists.
    * Errors encountered during the process.

To manage log file size and retention, the service implements log file rotation and archiving:
    * Log File Rotation: Renames and archives the current log file when it exceeds the size limit (256 MB by default).
    * Log File Archiving: Maintains a maximum of 4 archived log files, deleting the oldest files when this limit is exceeded.

### Dependencies

  * .NET Framework
  * Windows Operating System

### Author

[Nicolas G. Rico](nicolasgr2000@gmail.com)
