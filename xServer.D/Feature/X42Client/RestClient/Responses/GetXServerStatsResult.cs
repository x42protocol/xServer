using System.Collections.Generic;
using x42.Feature.X42Client.Models;

namespace x42.Feature.X42Client.RestClient.Responses
{
    public sealed class GetXServerStatsResult
    {
        public int Connected { get; set; }
        public List<xServerPeer> Nodes { get; set; }
    }
}