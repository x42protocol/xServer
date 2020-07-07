using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace x42.Feature.Database.Tables
{
    [Table("profile")]
    public class ProfileData
    {
        [Key]
        public string KeyAddress { get; set; }
        public string Name { get; set; }
        public string Signature { get; set; }
        public string TransactionId { get; set; }
    }
}