using System;
using System.Collections.Generic;

namespace X42.Controllers.Models
{
    /// <summary>
    ///     Class representing the ping result of the currently running node.
    /// </summary>
    public class PingResult
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PingResult" /> class.
        /// </summary>
        public PingResult() { }

        /// <summary>The node's version.</summary>
        public string Version { get; set; }

        /// <summary>System identifier of the node's process.</summary>
        public ulong BestBlockHeight { get; set; }
    }
}