using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace x42.Feature.Database.Tables
{
    [Table("profilereservation")]
    public class ProfileReservationData
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid ReservationId { get; set; }
        public string Name { get; set; }
        public string KeyAddress { get; set; }
        public string ReturnAddress { get; set; }
        public string Signature { get; set; }
        public int Status { get; set; }
        public string PriceLockId { get; set; }
        public long ReservationExpirationBlock { get; set; }
    }
}