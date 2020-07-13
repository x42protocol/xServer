namespace x42.Controllers.Results
{
    public class CreatePriceLockResult
    {
        public bool Success { get; set; }

        public string PriceLockId { get; set; }

        public decimal RequestAmount { get; set; }
        
        public int RequestAmountPair { get; set; }

        public decimal FeeAmount { get; set; }

        public string FeeAddress { get; set; }

        public decimal DestinationAmount { get; set; }

        public string DestinationAddress { get; set; }

        public string PriceLockSignature { get; set; }

        public string ResultMessage { get; set; }
    }
}