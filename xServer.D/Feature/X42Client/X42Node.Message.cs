using System.Threading.Tasks;

namespace x42.Feature.X42Client
{
    public sealed partial class X42Node
    {
        /// <summary>
        /// Verify the signature of a message.
        /// </summary>
        /// <returns>If verification was successful it will return true.</returns>
        public async Task<bool> VerifyMessageAsync(string externalAddress, string message, string signature)
        {
            bool response = await restClient.VerifySignedMessage(externalAddress, message, signature);

            return response;
        }
    }
}