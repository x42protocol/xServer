using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace X42.Feature.Database.Tables
{
    [Table("server")]
    class ServerData
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid Id { get; set; }
        public string Ip { get; set; }
        public string Port { get; set; }
        public string Address { get; set; }
        public string Signature { get; set; }
        public DateTime DateAdded { get; set; }
    }
}