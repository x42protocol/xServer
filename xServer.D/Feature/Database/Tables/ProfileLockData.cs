using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace x42.Feature.Database.Tables
{
    [Table("profilelock")]
    public class ProfileLockData
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid Id { get; set; }
        public string PriceLockId { get; set; }

        public decimal InitialRequestAmount { get; set; }

        public decimal FeeAmount { get; set; }

        public string FeeAddress { get; set; }

        public decimal DestinationAmount { get; set; }

        public string DestinationAddress { get; set; }

        public string PriceLockSignature { get; set; }
    }
}