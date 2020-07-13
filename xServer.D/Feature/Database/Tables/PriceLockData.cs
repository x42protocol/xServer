using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace x42.Feature.Database.Tables
{
    [Table("pricelock")]
    public class PriceLockData
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid PriceLockId { get; set; }

        public int Status { get; set; }

        public decimal RequestAmount { get; set; }

        public int RequestAmountPair { get; set; }

        public decimal FeeAmount { get; set; }

        public string FeeAddress { get; set; }

        public decimal DestinationAmount { get; set; }

        public string DestinationAddress { get; set; }

        public string TransacrionId { get; set; }

        public string PriceLockSignature { get; set; }

        public string PayeeSignature { get; set; }

        public long ExpireBlock { get; set; }
    }
}