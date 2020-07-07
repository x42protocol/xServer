using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace x42.Feature.Database.Tables
{
    [Table("server")]
    public class ServerData
    {
        [Key]
        public string KeyAddress { get; set; }
        public string PublicAddress { get; set; }
        public DateTime DateAdded { get; set; }
    }
}