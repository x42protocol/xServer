using NBitcoin;
using Newtonsoft.Json;
using System.Collections.Generic;
using x42.Controllers.Converters;

namespace x42.Feature.X42Client.RestClient.Responses
{
    /// <summary>
    /// Creates a more robust transaction model.
    /// </summary>
    public class RawTransactionResponse
    {
        public RawTransactionResponse() { }

        /// <summary>The transaction id.</summary>
        [JsonProperty(Order = 1, PropertyName = "txid")]
        public string TxId { get; set; }

        /// <summary>The transaction hash (differs from txid for witness transactions).</summary>
        [JsonProperty(Order = 2, PropertyName = "hash")]
        public string Hash { get; set; }

        /// <summary>The transaction version number (typically 1).</summary>
        [JsonProperty(Order = 3, PropertyName = "version")]
        public uint Version { get; set; }

        /// <summary>The serialized transaction size.</summary>
        [JsonProperty(Order = 4, PropertyName = "size")]
        public int Size { get; set; }

        /// <summary>The virtual transaction size (differs from size for witness transactions).</summary>
        [JsonProperty(Order = 5, PropertyName = "vsize")]
        public int VSize { get; set; }

        /// <summary>The transaction's weight (between vsize*4-3 and vsize*4).</summary>
        [JsonProperty(Order = 6, PropertyName = "weight", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Weight { get; set; }

        /// <summary>If nonzero, block height or timestamp when transaction is final.</summary>
        [JsonProperty(Order = 7, PropertyName = "locktime")]
        public uint LockTime { get; set; }

        /// <summary>A list of inputs.</summary>
        [JsonProperty(Order = 8, PropertyName = "vin")]
        public List<RTVin> VIn { get; set; }

        /// <summary>A list of outputs.</summary>
        [JsonProperty(Order = 9, PropertyName = "vout")]
        public List<RTVout> VOut { get; set; }

        /// <summary>The hash of the block containing this transaction.</summary>
        [JsonProperty(Order = 10, PropertyName = "blockhash", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string BlockHash { get; set; }

        /// <summary>The number of confirmations of the transaction.</summary>
        [JsonProperty(Order = 11, PropertyName = "confirmations", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? Confirmations { get; set; }

        /// <summary>The time the transaction was added to a block.</summary>
        [JsonProperty(Order = 12, PropertyName = "time", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public uint? Time { get; set; }

        /// <summary>The time the block was confirmed.</summary>
        [JsonProperty(Order = 13, PropertyName = "blocktime", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public uint? BlockTime { get; set; }

        /// <summary>The height of the block was confirmed.</summary>
        [JsonProperty(Order = 14, PropertyName = "blockheight", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? BlockHeight { get; set; }
    }

    /// <summary>
    /// A class describing a transaction input.
    /// </summary>
    public class RTVin
    {
        public RTVin() { }

        /// <summary>The scriptsig if this was a coinbase transaction.</summary>
        [JsonProperty(Order = 0, PropertyName = "coinbase", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Coinbase { get; set; }

        /// <summary>The transaction ID.</summary>
        [JsonProperty(Order = 1, PropertyName = "txid", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TxId { get; set; }

        /// <summary>The index of the output.</summary>
        [JsonProperty(Order = 2, PropertyName = "vout", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public uint? VOut { get; set; }

        /// <summary>The transaction's scriptsig.</summary>
        [JsonProperty(Order = 3, PropertyName = "scriptSig", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Script ScriptSig { get; set; }

        /// <summary>The transaction's sequence number. <see cref="https://bitcoin.org/en/developer-guide#locktime-and-sequence-number"/></summary>
        [JsonProperty(Order = 4, PropertyName = "sequence")]
        public uint Sequence { get; set; }
    }

    /// <summary>
    /// A class describing a transaction output.
    /// </summary>
    public class RTVout
    {
        public RTVout() { }

        /// <summary>The value of the output.</summary>
        [JsonConverter(typeof(BtcDecimalJsonConverter))]
        [JsonProperty(Order = 0, PropertyName = "value")]
        public decimal Value { get; set; }

        /// <summary>The index of the output.</summary>
        [JsonProperty(Order = 1, PropertyName = "n")]
        public int N { get; set; }

        /// <summary>The output's scriptpubkey.</summary>
        [JsonProperty(Order = 2, PropertyName = "scriptPubKey")]
        public ScriptPubKey ScriptPubKey { get; set; }
    }

    /// <summary>
    /// A class describing a transaction script.
    /// </summary>
    public class Script
    {
        public Script() { }

        /// <summary>The script's assembly.</summary>
        [JsonProperty(Order = 0, PropertyName = "asm")]
        public string Asm { get; set; }

        /// <summary>A hexadecimal representation of the script.</summary>
        [JsonProperty(Order = 1, PropertyName = "hex")]
        public string Hex { get; set; }
    }

    /// <summary>
    /// A class describing a ScriptPubKey.
    /// </summary>
    public class ScriptPubKey : Script
    {
        public ScriptPubKey() { }

        /// <summary>The number of required sigs.</summary>
        [JsonProperty(Order = 2, PropertyName = "reqSigs", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? ReqSigs { get; set; }

        /// <summary>The type of script.</summary>
        [JsonProperty(Order = 3, PropertyName = "type", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Type { get; set; }

        /// <summary>A list of output addresses.</summary>
        [JsonProperty(Order = 4, PropertyName = "addresses", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<string> Addresses { get; set; }

        /// <summary>
        /// A method that returns a script type description.
        /// </summary>
        /// <param name="template">A <see cref="ScriptTemplate"/> used for the script.</param>
        /// <returns>A string describin the script type.</returns>
        protected string GetScriptType(ScriptTemplate template)
        {
            if (template == null)
                return "nonstandard";
            switch (template.Type)
            {
                case TxOutType.TX_PUBKEY:
                    return "pubkey";

                case TxOutType.TX_PUBKEYHASH:
                    return "pubkeyhash";

                case TxOutType.TX_SCRIPTHASH:
                    return "scripthash";

                case TxOutType.TX_MULTISIG:
                    return "multisig";

                case TxOutType.TX_NULL_DATA:
                    return "nulldata";
            }

            return "nonstandard";
        }
    }
}
