using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace X42.Feature.Database.Tables
{
    [Table("servernode")]
    public class ServerNodeData
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid Id { get; set; }
        public string Ip { get; set; }
        public string Port { get; set; }
        public string PublicAddress { get; set; }
        public string Signature { get; set; }
        public string CollateralTX { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime LastSeen { get; set; }
        public long Priority { get; set; }
        public bool Active { get; set; }
    }
}