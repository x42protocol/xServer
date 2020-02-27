using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace X42.Feature.Database.Tables
{
    [Table("server")]
    public class ServerData
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public string PublicAddress { get; set; }
        public DateTime DateAdded { get; set; }
    }
}