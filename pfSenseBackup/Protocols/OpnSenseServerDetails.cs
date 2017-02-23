using System.Globalization;

namespace KoenZomers.Tools.opnSense.opnSenseBackup.Protocols
{
    /// <summary>
    /// Defines the details of a opnSense server
    /// </summary>
    public class OpnSenseServerDetails
    {
        /// <summary>
        /// Defines if HTTPS should be used (True) or HTTP (False)
        /// </summary>
        public bool UseHttps { get; set; }

        /// <summary>
        /// Defines the IP address or DNS name of the opnSense server
        /// </summary>
        public string ServerAddress { get; set; }

        /// <summary>
        /// Gets the base url of the opnSense server. Constructed automatically based on the ServerAddress and UseHttps properties.
        /// </summary>
        public string ServerBaseUrl => string.Format(CultureInfo.InvariantCulture, "{0}://{1}/", UseHttps ? "https" : "http", ServerAddress);

        /// <summary>
        /// Defines the timeout in milliseconds to allow for opnSense to respond before considering the connection stale and aborting
        /// </summary>
        public int? RequestTimeOut { get; set; }

        /// <summary>
        /// Version of opnSense (i.e. 17.1)
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// The username to use to connect to opnSense
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The password to use to connect to opnSense
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Defines if the statistics data should also be backed up (True) or not (False)
        /// </summary>
        public bool BackupStatisticsData { get; set; }

        /// <summary>
        /// Defines if the package information should also be backed up (True) or not (False)
        /// </summary>
        public bool BackupPackageInfo { get; set; }

        /// <summary>
        /// Defines if the backup should be encrypted by opnSense (True) or should be downloaded unencrypted (False)
        /// </summary>
        public bool EncryptBackup { get; set; }

        /// <summary>
        /// The password to use to encrypt the backup. Only applied if EncryptBackup is set to True.
        /// </summary>
        public string EncryptionPassword { get; set; }
    }
}
