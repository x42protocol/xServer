
using x42.Feature.Database.Tables;

namespace x42.Feature.Database.Entities
{
    public class XServerProfile
    {
        public string Name { get; set; }
        public string KeyAddress { get; set; }
        public string ReturnAddress { get; set; }
        public string Signature { get; set; }
        public int Status { get; set; }
        public string PriceLockId { get; set; }
        public int BlockConfirmed { get; set; }
        public bool Relayed { get; set; }

        public XServerProfile(ProfileData profileData)
        {

            Name = profileData.Name;
            KeyAddress = profileData.KeyAddress;
            ReturnAddress = profileData.ReturnAddress;
            Signature = profileData.Signature;
            Status = profileData.Status;
            PriceLockId = profileData.PriceLockId;
            BlockConfirmed = profileData.BlockConfirmed;
            Relayed = profileData.Relayed;

        }
    }
}
