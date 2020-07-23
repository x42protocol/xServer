using System.Collections.Generic;

namespace x42.Controllers.Results
{
    public class ProfilesResult
    {
        public string Name { get; set; }

        public string KeyAddress { get; set; }

        public string Signature { get; set; }

        public string PriceLockId { get; set; }

        public int Status { get; set; }

        public int BlockConfirmed { get; set; }

        public List<ProfileField> ProfileFields { get; set; }
    }
}