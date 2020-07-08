using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace x42.Feature.Database.Tables
{
    [Table("servernode")]
    public class ServerNodeData
    {
        [Key]
        public string ProfileName { get; set; }
        public int NetworkProtocol { get; set; }
        public string NetworkAddress { get; set; }
        public long NetworkPort { get; set; }
        public string ServerKeyAddress { get; set; }
        public int Tier { get; set; }
        public string Signature { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime LastSeen { get; set; }
        public long Priority { get; set; }
        public bool Active { get; set; }
        public bool Relayed { get; set; }
    }
}