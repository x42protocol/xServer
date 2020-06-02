using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace x42.Feature.Database.Tables
{
    [Table("servernode")]
    public class ServerNodeData
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string NetworkAddress { get; set; }
        public long NetworkPort { get; set; }
        public string PublicAddress { get; set; }
        public int Tier { get; set; }
        public string Signature { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime LastSeen { get; set; }
        public long Priority { get; set; }
        public bool Active { get; set; }
    }
}