namespace x42.Feature.PriceLock
{
    /// <summary>Price lock status.</summary>
    public enum Status
    {
        Rejected = 0,
        New = 1,
        WaitingForConfirmation = 2,
        Confirmed = 3
    }
}
