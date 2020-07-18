using NBitcoin;
using System.Collections.Generic;

namespace x42.Feature.X42Client.RestClient.Responses
{
    /// <summary>
    /// A model class to be returned when the user sends a transaction successfully.
    /// </summary>
    public class WalletSendTransactionModel
    {
        /// <summary>
        /// The transaction id.
        /// </summary>
        public uint256 TransactionId { get; set; }

        /// <summary>
        /// The list of outputs in this transaction.
        /// </summary>
        public ICollection<TransactionOutputModel> Outputs { get; set; }
    }

    /// <summary>
    /// A simple transaction output.
    /// </summary>
    public class TransactionOutputModel
    {
        /// <summary>
        /// The output address in Base58.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// The amount associated with the output.
        /// </summary>
        public Money Amount { get; set; }

        /// <summary>
        /// The data encoded in the OP_RETURN script
        /// </summary>
        public string OpReturnData { get; set; }
    }
}