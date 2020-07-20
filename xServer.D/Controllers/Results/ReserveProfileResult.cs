namespace x42.Controllers.Results
{
    public class ReserveProfileResult
    {
        public bool Success { get; set; }

        public string ResultMessage { get; set; }

        public string PriceLockId { get; set; }

        public int Status { get; set; }
    }
}