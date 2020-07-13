using Newtonsoft.Json;

namespace x42.Feature.PriceLock.Results
{
    public partial class ValidatePriceLockPayeeResult
    {
        public bool Success { get; set; }
        public string ResultMessage { get; set; }
    }
}
