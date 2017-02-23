﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace KoenZomers.Tools.opnSense.opnSenseBackup
{
    /// <summary>
    /// Application to retrieve the backup file from a opnSense installation
    /// </summary>
    internal class Program
    {
        #region Constants

        /// <summary>
        /// Defines the opnSense version to use if not explicitly specified
        /// </summary>
        private const string DefaultOpnSenseVersion = "17.1";

        #endregion

        #region Properties

        /// <summary>
        /// Details of the opnSense server to communicate with
        /// </summary>
        public static Protocols.OpnSenseServerDetails OpnSenseServerDetails = new Protocols.OpnSenseServerDetails();

        /// <summary>
        /// The filename which to save the backup file to
        /// </summary>
        public static string OutputFileName { get; set; }

        /// <summary>
        /// Indicates if no output should be sent (true)
        /// </summary>
        public static bool UseSilentMode { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Application entry point
        /// </summary>
        /// <param name="args">Command line arguments</param>
        private static void Main(string[] args)
        {
            // Parse the provided arguments
            if (args.Length > 0)
            {
                ParseArguments(args);
            }

            WriteOutput();

            var appVersion = Assembly.GetExecutingAssembly().GetName().Version;

            WriteOutput("opnSense Backup Tool v{0}.{1}.{2} by Koen Zomers", new object[] { appVersion.Major, appVersion.Minor, appVersion.Build });
            WriteOutput();

            // Check if parameters have been provided
            if (args.Length == 0)
            {
                // No arguments have been provided
                WriteOutput("ERROR: No arguments provided");
                WriteOutput();

                DisplayHelp();

                Environment.Exit(1);
            }

            // Make sure the provided arguments have been provided
            if (string.IsNullOrEmpty(OpnSenseServerDetails.Username) ||
                string.IsNullOrEmpty(OpnSenseServerDetails.Password) ||
                string.IsNullOrEmpty(OpnSenseServerDetails.ServerAddress))
            {
                WriteOutput("ERROR: Not all required options have been provided");

                DisplayHelp();

                Environment.Exit(1);
            }

            // Check if the output filename parsed resulted in an error
            if (!string.IsNullOrEmpty(OutputFileName) && OutputFileName.Equals("ERROR", StringComparison.InvariantCultureIgnoreCase))
            {
                WriteOutput("ERROR: Provided output filename contains illegal characters");

                Environment.Exit(1);
            }

            // Retrieve the backup file from opnSense
            RetrieveBackupFile();

            Environment.Exit(0);
        }

        /// <summary>
        /// Retrieves the backup file from opnSense
        /// </summary>
        private static void RetrieveBackupFile()
        {
            if (OpnSenseServerDetails.UseHttps)
            {
                // Ignore all certificate related errors
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            }

            // Create a cookie container to hold the session cookies
            var cookieJar = new CookieContainer();

            // Define the protocol implementation to use to communicate with opnSense
            Protocols.IOpnSenseProtocol opnSenseProtocol = null;
            switch (OpnSenseServerDetails.Version)
            {
                case "17.1":
                    opnSenseProtocol = new Protocols.OpnSenseVersion171();
                    break;

                default:
                    WriteOutput("Unsupported opnSense version provided ({0})", new object[] { OpnSenseServerDetails.Version });
                    Environment.Exit(1);
                    break;
            }

            // Execute the communication with opnSense through the protocol implementation
            Protocols.OpnSenseBackupFile opnSenseBackupFile = null;
            try
            {
                opnSenseBackupFile = opnSenseProtocol.Execute(OpnSenseServerDetails, cookieJar, OpnSenseServerDetails.RequestTimeOut.GetValueOrDefault(60000));
            }
            catch (Exception e)
            {
                WriteOutput("Error: {0}", new object[] { e.Message });
                Environment.Exit(1);
            }

            // Verify that the backup file returned contains content
            if (opnSenseBackupFile == null || string.IsNullOrEmpty(opnSenseBackupFile.FileContents))
            {
                WriteOutput("No valid backup contents returned");
                Environment.Exit(1);
            }

            // Define the full path to the file to store the backup in. By default this will be in the same directory as this application runs from using the filename provided by opnSense, unless otherwise specified using the -o parameter.
            string outputDirectory;
            if (string.IsNullOrEmpty(OutputFileName))
            {
                // -o flag has not been provided, store the file in the same directory as this tool runs using the same filename as provided by opnSense
                outputDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), opnSenseBackupFile.FileName);
            }
            else
            {
                // -o flag has been provided, check if just a path was provided or a path including filename
                if (Directory.Exists(OutputFileName))
                {
                    // Path was provided, use the filename as provided by opnSense to store it in the by -o provided path
                    outputDirectory = Path.Combine(OutputFileName, opnSenseBackupFile.FileName);
                }
                else
                {
                    // Complete path including filename has been provided with the -o flag, use that to store the backup
                    outputDirectory = OutputFileName;
                }
            }

            WriteOutput(string.Concat("Saving backup file to ", outputDirectory));

            // Store the backup contents in the file
            WriteBackupToFile(outputDirectory, opnSenseBackupFile.FileContents);
            
            WriteOutput();
            WriteOutput("DONE");
        }
        
        /// <summary>
        /// Writes the backup content to a file
        /// </summary>
        /// <param name="filename">Full file path where to store the file</param>
        /// <param name="backupContents">Contents of the backup to write to the file</param>
        private static void WriteBackupToFile(string filename, string backupContents)
        {
            try
            {
                File.WriteAllText(filename, backupContents, Encoding.UTF8);
            }
            catch (UnauthorizedAccessException)
            {
                WriteOutput("!! Unable to write the backup file to {0}. Make sure the account you use to run this tool has write rights to this location.", new object[] { filename });
            }
            catch(Exception ex)
            {
                WriteOutput("!! Unable to write the backup file to {0}. The error that occurred was: '{1}'", new object[] { filename, ex.Message });
            }
        }

        /// <summary>
        /// Parses all provided arguments
        /// </summary>
        /// <param name="args">String array with arguments passed to this console application</param>
        private static void ParseArguments(IList<string> args)
        {
            UseSilentMode = args.Contains("-silent");

            if (args.Contains("-u"))
            {
                OpnSenseServerDetails.Username = args[args.IndexOf("-u") + 1];
            }

            if (args.Contains("-p"))
            {
                OpnSenseServerDetails.Password = args[args.IndexOf("-p") + 1];
            }

            if (args.Contains("-s"))
            {
                OpnSenseServerDetails.ServerAddress = args[args.IndexOf("-s") + 1];
            }

            if (args.Contains("-o"))
            {
                var outputTo = args[args.IndexOf("-o") + 1];

                // Verify that the target filename does not contain any invalid characters
                try
                {
                    new FileInfo(outputTo);
                    OutputFileName = outputTo;
                }
                catch (ArgumentException)
                {
                    OutputFileName = "ERROR";
                } 
                catch (NotSupportedException)
                {
                    OutputFileName = "ERROR";
                }                
            }

            if (args.Contains("-e"))
            {
                OpnSenseServerDetails.EncryptBackup = true;
                OpnSenseServerDetails.EncryptionPassword = args[args.IndexOf("-e") + 1];
            }

            if (args.Contains("-t"))
            {
                int timeout;
                if(int.TryParse(args[args.IndexOf("-t") + 1], out timeout))
                {
                    // Input is in seconds, value is in milliseconds, so multiply with 1000
                    OpnSenseServerDetails.RequestTimeOut = timeout * 1000;
                }                
            }

            OpnSenseServerDetails.Version = args.Contains("-v") ? args[args.IndexOf("-v") + 1] : DefaultOpnSenseVersion;

            OpnSenseServerDetails.BackupStatisticsData = !args.Contains("-norrd");
            OpnSenseServerDetails.UseHttps = args.Contains("-usessl");
        }

        /// <summary>
        /// Shows the syntax
        /// </summary>
        private static void DisplayHelp()
        {
            WriteOutput("Usage:");
            WriteOutput("   opnSenseBackup.exe -u <username> -p <password> -s <serverip> [-v <opnSense Version> -o <filename> -usessl -norrd]");
            WriteOutput();
            WriteOutput("u: Username of the account to use to log on to opnSense");
            WriteOutput("p: Password of the account to use to log on to opnSense");
            WriteOutput("s: IP address or DNS name of the opnSense server");
            WriteOutput("v: opnSense version. Supported is 17.1 (17.1 = default, optional)");
            WriteOutput("o: Folder or complete path where to store the backup file (optional)");
            WriteOutput("e: Have opnSense encrypt the backup using this password (optional)");
            WriteOutput("t: Timeout in seconds for opnSense to retrieve the backup (60 seconds = default, optional)");
            WriteOutput("usessl: if provided https will be used to connect to opnSense instead of http");
            WriteOutput("norrd: if provided no RRD statistics data will be included");
            WriteOutput("silent: if provided no output will be shown");
            WriteOutput();
            WriteOutput("Example:");
            WriteOutput("   opnSenseBackup.exe -u admin -p mypassword -s 192.168.0.1");
            WriteOutput("   opnSenseBackup.exe -u admin -p mypassword -s 192.168.0.1:8000");
            WriteOutput("   opnSenseBackup.exe -u admin -p mypassword -s 192.168.0.1 -usessl");
            WriteOutput("   opnSenseBackup.exe -u admin -p mypassword -s 192.168.0.1 -o c:\\backups -norrd");
            WriteOutput("   opnSenseBackup.exe -u admin -p mypassword -s 192.168.0.1 -o c:\\backups\\opnSense.xml -norrd");
            WriteOutput("   opnSenseBackup.exe -u admin -p mypassword -s 192.168.0.1 -o \"c:\\my backups\"");
            WriteOutput("   opnSenseBackup.exe -u admin -p mypassword -s 192.168.0.1 -e \"mypassword\"");
            WriteOutput("   opnSenseBackup.exe -u admin -p mypassword -s 192.168.0.1 -t 120");
            WriteOutput();
            WriteOutput("Output:");
            WriteOutput("   A timestamped file containing the backup will be created within this directory unless -o is being specified");
            WriteOutput();
        }

        /// <summary>
        /// Writes output to the console based on the silent flag being enabled
        /// </summary>
        internal static void WriteOutput()
        {
            WriteOutput(string.Empty);
        }

        /// <summary>
        /// Writes output to the console based on the silent flag being enabled
        /// </summary>
        /// <param name="text">Text to write to the console</param>
        internal static void WriteOutput(string text)
        {
            // Check if silent mode is enabled, if so, return and do not write the output
            if (UseSilentMode) return;

            // Silent mode is not enabled, write the output
            Console.WriteLine(text);
        }

        /// <summary>
        /// Writes output to the console based on the silent flag being enabled
        /// </summary>
        /// <param name="format">Formatted string to write to the console</param>
        /// <param name="args">Arguments to inser into the formatted string</param>
        internal static void WriteOutput(string format, object[] args)
        {            
            WriteOutput(string.Format(format, args));
        }

        /// <summary>
        /// Sends a POST request using the multipart form data method to download the opnSense backup file
        /// </summary>
        /// <param name="url">Url to POST the backup file request to</param>
        /// <param name="userName">userName</param>
        /// <param name="userPassword">userPassword</param>
        /// <param name="formFields">Dictonary with key/value pairs containing the forms data to POST to the webserver</param>
        /// <param name="cookieContainer">Cookies which have been recorded for this session</param>
        /// <param name="filename">Filename of the download as provided by opnSense (out parameter)</param>
        /// <returns>The website contents returned by the webserver after posting the data</returns>
        public static string DownloadBackupFile(string url, string userName, string userPassword, Dictionary<string, string> formFields, CookieContainer cookieContainer, out string filename)
        {
            filename = null;

            // Define the form separator to use in the POST request
            const string formDataBoundary = "---------------------------7dc1873b1609fa";

            // Construct the POST request which performs the login
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.Accept = "*/*";
            request.ServicePoint.Expect100Continue = false;
            request.CookieContainer = cookieContainer;

            SetBasicAuthHeader(request, userName, userPassword);

            // Construct POST data
            var postData = new StringBuilder();
            foreach (var formField in formFields)
            {
                postData.AppendLine(string.Concat("--", formDataBoundary));
                postData.AppendLine(string.Format("Content-Disposition: form-data; name=\"{0}\"", formField.Key));
                postData.AppendLine();
                postData.AppendLine(formField.Value);
            }
            postData.AppendLine(string.Concat("--", formDataBoundary, "--"));

            // Convert the POST data to a byte array
            var postDataByteArray = Encoding.UTF8.GetBytes(postData.ToString());

            // Set the ContentType property of the WebRequest
            request.ContentType = string.Concat("multipart/form-data; boundary=", formDataBoundary);

            // Set the ContentLength property of the WebRequest.
            request.ContentLength = postDataByteArray.Length;

            // Get the request stream
            var dataStream = request.GetRequestStream();

            // Write the POST data to the request stream
            dataStream.Write(postDataByteArray, 0, postDataByteArray.Length);

            // Close the Stream object
            dataStream.Close();

            // Receive the response from the webserver
            HttpWebResponse response = null;

            try
            {
                response = request.GetResponse() as HttpWebResponse;
            }
            catch (WebException wex)
            {

                response = (HttpWebResponse)wex.Response;
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    WriteOutput("ERROR: Credentials incorrect");
                    Environment.Exit(1);
                }

                WriteOutput("ERROR: {0}", new object[] { wex.Message });
                Environment.Exit(1);

            }
            catch (Exception ex)
            {
                WriteOutput("ERROR: {0}", new object[] { ex.Message });
                Environment.Exit(1);
            }

            // Make sure the webserver has sent a response
            if (response == null) return null;

            dataStream = response.GetResponseStream();

            // Make sure the datastream with the response is available
            if (dataStream == null) return null;

            // Get the content-disposition header and use a regex on its value to find out what filename opnSense assigns to the download
            var contentDispositionHeader = response.Headers["Content-Disposition"];
            var filenameRegEx = Regex.Match(contentDispositionHeader, @"filename=(?<filename>.*)(?:\s|\z)");

            if (filenameRegEx.Success && filenameRegEx.Groups["filename"].Success)
            {
                filename = filenameRegEx.Groups["filename"].Value;
            }

            var reader = new StreamReader(dataStream);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Forcing basic http authentication for HttpWebRequest (in .NET/C#) 
        /// http://blog.kowalczyk.info/article/Forcing-basic-http-authentication-for-HttpWebReq.html
        /// </summary>
        /// <param name="req">HttpWebRequest to add Authorization Header</param>
        /// <param name="userName">UserName of opnSense</param>
        /// <param name="userPassword">Password of opnSense</param>
        public static void SetBasicAuthHeader(HttpWebRequest req, string userName, string userPassword)
        {
            string authInfo = userName + ":" + userPassword;
            authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
            req.Headers["Authorization"] = "Basic " + authInfo;
        }

        #endregion
    }
}
