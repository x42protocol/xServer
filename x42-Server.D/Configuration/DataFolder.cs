using System.IO;

namespace X42.Configuration
{
    /// <summary>
    ///     Contains path locations to folders and files on disk.
    ///     Used by various components of the x42 server.
    /// </summary>
    /// <remarks>
    ///     Location name should describe if its a file or a folder.
    ///     File location names end with "File" (i.e AddrMan[File]).
    ///     Folder location names end with "Path" (i.e CoinView[Path]).
    /// </remarks>
    public class DataFolder
    {
        /// <summary>
        ///     Initializes the path locations.
        /// </summary>
        /// <param name="path">The data directory root path.</param>
        public DataFolder(string path)
        {
            LogPath = Path.Combine(path, "Logs");
            ApplicationsPath = Path.Combine(path, "apps");
            RootPath = path;
        }

        /// <summary>
        ///     The DataFolder's path.
        /// </summary>
        public string RootPath { get; }

        /// <summary>Path to log files.</summary>
        /// <seealso cref="Logging.LoggingConfiguration" />
        public string LogPath { get; internal set; }

        /// <summary>Path to x42 Server applications</summary>
        public string ApplicationsPath { get; internal set; }
    }
}