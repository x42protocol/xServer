using System.Threading.Tasks;
using x42.Feature.Network;
using x42.Feature.X42Client.RestClient.Responses;

namespace x42.Feature.PriceLock
{
    /// <inheritdoc />
    /// <summary>
    ///     Price Lock Validation fucntions.
    /// </summary>
    public class PriceLockValidation
    {
        private readonly NetworkFeatures network;

        public PriceLockValidation(NetworkFeatures networkFeatures)
        {
            network = networkFeatures;
        }

        public async Task<bool> IsPayeeSignatureValid(RawTransactionResponse rawTransaction, string pricelockId, string signature)
        {
            bool result = false;
            foreach (var tx in rawTransaction.VIn)
            {
                var inputTransaction = await network.GetRawTransaction(tx.TxId, true);
                foreach (var transactionOutputs in inputTransaction.VOut)
                {
                    foreach (string address in transactionOutputs.ScriptPubKey.Addresses)
                    {
                        bool foundValidAddress = await network.VerifySenderPriceLockSignature(address, pricelockId, signature);
                        if (foundValidAddress)
                        {
                            return true;
                        }
                    }
                }
            }
            return result;
        }

    }
}
