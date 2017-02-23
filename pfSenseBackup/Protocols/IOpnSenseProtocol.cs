using System.Net;

namespace KoenZomers.Tools.opnSense.opnSenseBackup.Protocols
{
    /// <summary>
    /// Defines the interface to which opnSense protocol handlers must comply
    /// </summary>
    public interface IOpnSenseProtocol
    {
        /// <summary>
        /// Connects with the specified opnSense server using the current protocol implementation and returns the backup file contents
        /// </summary>
        /// <param name="opnSenseServer">opnSense server details which identifies which opnSense server to connect to</param>
        /// <param name="cookieJar">Cookie container to use through the communication with opnSense</param>
        /// <param name="timeout">Timeout in milliseconds on how long requests to opnSense may take. Default = 60000 = 60 seconds.</param>
        /// <returns>OpnSenseBackupFile instance containing the retrieved backup content from opnSense</returns>
        OpnSenseBackupFile Execute(OpnSenseServerDetails opnSenseServer, CookieContainer cookieJar, int timeout = 60000);
    }
}
