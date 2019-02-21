using System;
using System.Collections.Generic;

namespace X42.Controllers.Models
{
    /// <summary>
    ///     Class representing the status of the currently running node.
    /// </summary>
    public class StatusModel
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="StatusModel" /> class.
        /// </summary>
        public StatusModel()
        {
            EnabledFeatures = new List<string>();
        }

        /// <summary>The node's user agent that will be shared with peers in the version handshake.</summary>
        public string Agent { get; set; }

        /// <summary>The node's version.</summary>
        public string Version { get; set; }

        /// <summary>The network the current node is running on.</summary>
        public string MasterNode { get; set; }

        /// <summary>The coin ticker to use with external applications.</summary>
        public string CoinTicker { get; set; }

        /// <summary>System identifier of the node's process.</summary>
        public int ProcessId { get; set; }

        /// <summary>A collection of all the features enabled by this node.</summary>
        public List<string> EnabledFeatures { get; set; }

        /// <summary>The path to the directory where the data is saved.</summary>
        public string DataDirectoryPath { get; set; }

        /// <summary>Time this node has been running.</summary>
        public TimeSpan RunningTime { get; set; }

        /// <summary>The node's protocol version</summary>
        public uint ProtocolVersion { get; set; }

        /// <summary>Returns the status of the node.</summary>
        public string State { get; set; }
    }
}