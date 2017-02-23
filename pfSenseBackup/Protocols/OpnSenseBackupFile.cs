namespace KoenZomers.Tools.opnSense.opnSenseBackup.Protocols
{
    /// <summary>
    /// Defines a opnSense Backup File
    /// </summary>
    public class OpnSenseBackupFile
    {
        /// <summary>
        /// The filename for the backup file
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// The contents of the backup file
        /// </summary>
        public string FileContents { get; set; }
    }
}
