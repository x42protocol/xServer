using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace X42.Feature.Database.Tables
{
    [Table("servernode")]
    class ServerNodeData
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid Id { get; set; }
        public string Ip { get; set; }
        public string Port { get; set; }
        public string HAddress { get; set; }
        public string CAddress { get; set; }
        public string Signature { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime LastSeen { get; set; }
        public long Priority { get; set; }
        public bool Active { get; set; }
    }
}