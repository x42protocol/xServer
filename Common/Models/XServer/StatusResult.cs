using System.Collections.Generic;

namespace Common.Models.XServer
{
    /// <summary>
    ///     Class representing the status of the currently running node.
    /// </summary>
    public class StatusResult
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="StatusResult" /> class.
        /// </summary>
        public StatusResult()
        {
            EnabledFeatures = new List<string>();
        }

        /// <summary>The nodes name (Profile name).</summary>
        public string Name { get; set; }

        /// <summary>The nodes key address.</summary>
        public string FeeAddress { get; set; }

        /// <summary>Returns the public key for the xServer.</summary>
        public string PublicKey { get; set; }

        /// <summary>The node's version.</summary>
        public string Version { get; set; }

        /// <summary>System identifier of the node's process.</summary>
        public int ProcessId { get; set; }

        /// <summary>A collection of all the features enabled by this node.</summary>
        public List<string> EnabledFeatures { get; set; }

        /// <summary>The path to the directory where the data is saved.</summary>
        public string DataDirectoryPath { get; set; }

        /// <summary>Time this node has been running in seconds.</summary>
        public double RunningTimeSeconds { get; set; }

        /// <summary>The node's protocol version</summary>
        public uint ProtocolVersion { get; set; }

        /// <summary>Returns the status of the node.</summary>
        public string State { get; set; }

        /// <summary>Returns the status of the database.</summary>
        public bool DatabaseConnected { get; set; }

        /// <summary>Returns the xServer stats.</summary>
        public RuntimeStats Stats { get; set; }
    }
}