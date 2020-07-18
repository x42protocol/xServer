namespace x42.Feature.PriceLock
{
    /// <summary>Payment Error Codes.</summary>
    public enum PaymentErrorCodes
    {
        None = 0,
        PriceLockNotFound = 1,
        NotNew = 2,
        InvalidSignature = 3,
        TransactionError = 4,
        TransactionDestNotFound = 5,
        TransactionFeeNotFound = 6, 
        AlreadyExists = 7
    }
}
