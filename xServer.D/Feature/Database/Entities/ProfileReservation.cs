using System;

namespace x42.Feature.Database.Entities
{
    public class ProfileReservation
    {
        public Guid ReservationId { get; set; }
        public string Name { get; set; }
        public string KeyAddress { get; set; }
        public string ReturnAddress { get; set; }
        public string Signature { get; set; }
        public int Status { get; set; }
        public string PriceLockId { get; set; }
        public int ReservationExpirationBlock { get; set; }
        public bool Relayed { get; set; }
    }
}
