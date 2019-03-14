using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace x42.Feature.Database.Tables
{
    [Table("masternodes")]
    class MasterNode
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid Id { get; set; }
        public string Ip { get; set; }
        public string Port { get; set; }
        public string Address { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime LastSeen { get; set; }
    }
}