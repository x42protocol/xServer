using System.Threading.Tasks;
using x42.Feature.X42Client.RestClient.Requests;
using x42.Feature.X42Client.RestClient.Responses;

namespace x42.Feature.X42Client
{
    public sealed partial class X42Node
    {
        /// <summary>
        /// Verify the signature of a message.
        /// </summary>
        /// <returns>If verification was successful it will return true.</returns>
        public async Task<SignMessageResult> SignMessageAsync(SignMessageRequest signMessageRequest)
        {
            SignMessageResult response = await restClient.SignMessage(signMessageRequest);

            return response;
        }

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