using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace KoenZomers.Tools.opnSense.opnSenseBackup.Protocols
{
    /// <summary>
    /// Implementation of the opnSense protocol for version 17.1
    /// </summary>
    public class OpnSenseVersion171 : IOpnSenseProtocol
    {
        /// <summary>
        /// Connects with the specified opnSense server using the v17.1 protocol implementation and returns the backup file contents
        /// </summary>
        /// <param name="opnSenseServer">opnSense server details which identifies which opnSense server to connect to</param>
        /// <param name="cookieJar">Cookie container to use through the communication with opnSense</param>
        /// <param name="timeout">Timeout in milliseconds on how long requests to opnSense may take. Default = 60000 = 60 seconds.</param>
        /// <returns>OpnSenseBackupFile instance containing the retrieved backup content from opnSense</returns>
        public OpnSenseBackupFile Execute(OpnSenseServerDetails opnSenseServer, CookieContainer cookieJar, int timeout = 60000)
        {
            Program.WriteOutput("Connecting using protocol version {0}", new object[] { opnSenseServer.Version });

            // Create a session on the opnSense webserver
            var loginPageContents = HttpUtility.HttpGetLoginPageContents(opnSenseServer.ServerBaseUrl, cookieJar, timeout);

            // Check if a response was returned from the login page request
            if (string.IsNullOrEmpty(loginPageContents))
            {
                throw new ApplicationException("Unable to retrieve login page contents");
            }

            Program.WriteOutput("Authenticating");

            // Use a regular expression to fetch the anti cross site scriping token from the HTML
            var xssToken = Regex.Match(loginPageContents, "<input.+?type=['\"]hidden['\"].+?id=['\"]_+?opnsense_csrf['\"].+?name=['\"](?<xsstokenname>.*?)['\"].+?value=['\"](?<xsstokenvalue>.*?)['\"].+?/>", RegexOptions.IgnoreCase);

            // Verify that the anti XSS token was found
            if (!xssToken.Success)
            {
                throw new ApplicationException("Unable to retrieve Cross Site Request Forgery token from login page");
            }
            
            // Authenticate the session
            var authenticationResult = HttpUtility.AuthenticateViaUrlEncodedFormMethod(string.Concat(opnSenseServer.ServerBaseUrl, "index.php"),
                                                                                       new Dictionary<string, string>
                                                                                       {
                                                                                            { xssToken.Groups["xsstokenname"].Value, xssToken.Groups["xsstokenvalue"].Value },
                                                                                            { "usernamefld", System.Web.HttpUtility.UrlEncode(opnSenseServer.Username) }, 
                                                                                            { "passwordfld", System.Web.HttpUtility.UrlEncode(opnSenseServer.Password) }, 
                                                                                            { "login", "1" }
                                                                                       },
                                                                                       cookieJar,
                                                                                       timeout);

            // Verify if the username/password combination was valid by examining the server response
            if (authenticationResult.Contains("Username or Password incorrect"))
            {
                throw new ApplicationException("ERROR: Credentials incorrect");
            }

            Program.WriteOutput("Requesting backup file");

            // Get the backup page contents for the xsrf token
            var backupPageUrl = string.Concat(opnSenseServer.ServerBaseUrl, "diag_backup.php");

            var backupPageContents = HttpUtility.HttpGetLoginPageContents(backupPageUrl, cookieJar, timeout);

            // Check if a response was returned from the login page request
            if (string.IsNullOrEmpty(backupPageContents))
            {
                throw new ApplicationException("Unable to retrieve backup page contents");
            }

            // Use a regular expression to fetch the anti cross site scriping token from the HTML
            xssToken = Regex.Match(backupPageContents, "<input.+?type=['\"]hidden['\"].+?id=['\"]_+?opnsense_csrf['\"].+?name=['\"](?<xsstokenname>.*?)['\"].+?value=['\"](?<xsstokenvalue>.*?)['\"].+?/>", RegexOptions.IgnoreCase);

            // Verify that the anti XSS token was found
            if (!xssToken.Success)
            {
                throw new ApplicationException("Unable to retrieve Cross Site Request Forgery token from backup page");
            }

            Program.WriteOutput("Retrieving backup file");

            var downloadArgs = new Dictionary<string, string>
                {
                    { xssToken.Groups["xsstokenname"].Value, xssToken.Groups["xsstokenvalue"].Value },
                    { "donotbackuprrd", opnSenseServer.BackupStatisticsData ? "" : "on" },
                    { "encrypt", opnSenseServer.EncryptBackup ? "on" : "" },
                    { "encrypt_password", opnSenseServer.EncryptionPassword },
                    { "encrypt_passconf", opnSenseServer.EncryptionPassword },
                    { "download", "Download configuration" },
                    { "restorearea", "" },
                    { "rebootafterrestore", "on" },
                    { "decrypt_password", "" },
                    { "decrypt_passconf", "" }
                };

            string filename;
            var opnSenseBackupFile = new OpnSenseBackupFile
            {
                FileContents = HttpUtility.DownloadBackupFile(  backupPageUrl,
                                                                downloadArgs,
                                                                cookieJar,
                                                                out filename,
                                                                timeout,
                                                                backupPageUrl),
                FileName = filename
            };
            return opnSenseBackupFile;
        }
    }
}
