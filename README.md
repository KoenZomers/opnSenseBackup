# opnSenseBackup
opnSense Backup allows you to backup the complete configuration of your opnSense server using this command line Windows application. It is easy to include this in a larger script for your backups and schedule it i.e. with the Windows Task Scheduler.

## Download

[Download the latest version](https://github.com/KoenZomers/opnSenseBackup/raw/master/Releases/opnSenseBackupv1.0.zip)

## Release Notes
 
1.0 - released February 23, 2017 - [download](https://github.com/KoenZomers/opnSenseBackup/raw/master/Releases/opnSenseBackupv1.0.zip) - 10 kb

- Initial release

[Version History](https://github.com/KoenZomers/opnSenseBackup/blob/master/VersionHistory.md)

## System Requirements

This tool requires the Microsoft .NET 4.6 framework to be installed on your Windows client or Windows server operating system. For a full list of operating systems on which this framework can be installed, see: https://msdn.microsoft.com/en-us/library/8z6watww(v=vs.110).aspx. Basically it can be installed on Windows Vista SP2 or later or Windows Server 2008 SP2 or later.

## Usage Instructions

1. Copy opnSenseBackup.exe to any location on a Windows machine where you want to use the tool
2. Run opnSenseBackup.exe to see the command line options
3. Run opnSenseBackup.exe with the appropriate command line options to connect to your opnSense server and download the backup

![](./Documentation/Images/Help.png)

![](./Documentation/Images/SampleExecution.png)

## Feedback

Any kind of feedback is welcome! Feel free to drop me an e-mail at mail@koenzomers.nl. Please note that I don't use opnSense myself, so I rely on you guys to tell me if a new version of opnSense breaks my tool. I'll be happy to try to adjust my tool accordingly, just let me know through e-mail.
