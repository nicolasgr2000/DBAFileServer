# DBAFileServer

DBAFileServer is a Windows Service designed to copy backup files (.bak) from a specified source directory to a destination directory. This service periodically checks the source directory and replicates its structure and contents to the destination directory, omitting any files that already exist in the destination.
Features

Automated Backup File Copying: Copies all .bak files from the source to the destination directory while maintaining the directory structure.
Configurable Source and Destination Paths: The service reads the source and destination paths from a configuration file.
Logging: Logs all activities, including start and stop events, file copying actions, and any errors encountered, to the Windows Event Log.
Scheduled Execution: Runs every 3 hours by default.

## Configuration
### App.config

The App.config file should contain the following keys to specify the source and destination directories:
```xml
    <configuration>
      <appSettings>
        <add key="sourceFolder" value="E:\Test"/>
        <add key="destinationFolder" value="E:\Pruebas"/>
      </appSettings>
    </configuration>    
```

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

### Dependencies

  * .NET Framework
  * Windows Operating System

### Author

[Nicolas G. Rico](nicolasgr2000@gmail.com)
