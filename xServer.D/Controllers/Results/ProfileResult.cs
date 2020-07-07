using System.Collections.Generic;

namespace x42.Controllers.Results
{
    public class ProfileResult
    {
        public string Name { get; set; }

        public string KeyAddress { get; set; }

        public string Signature { get; set; }

        public string TransactionId { get; set; }

        public List<ProfileField> ProfileFields { get; set; }
    }
}