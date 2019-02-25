using System;
using System.Reflection;
using NBitcoin;
using NBitcoin.OpenAsset;
using Newtonsoft.Json;

namespace X42.Utilities.JsonConverters
{
    /// <summary>
    ///     Converter used to convert an object implementing <see cref="ICoin" /> to and from JSON.
    /// </summary>
    /// <seealso cref="Newtonsoft.Json.JsonConverter" />
    public class CoinJsonConverter : JsonConverter
    {
        public CoinJsonConverter(Network network)
        {
            Network = network;
        }

        public Network Network { get; set; }

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return typeof(ICoin).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            return reader.TokenType == JsonToken.Null ? null : serializer.Deserialize<CoinJson>(reader).ToCoin();
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, new CoinJson((ICoin) value, Network));
        }
    }

    public class CoinJson
    {
        public CoinJson()
        {
        }

        public CoinJson(ICoin coin, Network network)
        {
            TransactionId = coin.Outpoint.Hash;
            Index = coin.Outpoint.N;
            ScriptPubKey = coin.TxOut.ScriptPubKey;

            if (coin is ScriptCoin) RedeemScript = ((ScriptCoin) coin).Redeem;

            if (coin is Coin) Value = ((Coin) coin).Amount;

            if (coin is ColoredCoin cc)
            {
                AssetId = cc.AssetId.GetWif(network);
                Quantity = cc.Amount.Quantity;
                Value = cc.Bearer.Amount;
                if (cc.Bearer is ScriptCoin scc) RedeemScript = scc.Redeem;
            }
        }

        public uint256 TransactionId { get; set; }

        public uint Index { get; set; }

        public Money Value { get; set; }

        public Script ScriptPubKey { get; set; }

        public Script RedeemScript { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public BitcoinAssetId AssetId { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long Quantity { get; set; }

        public ICoin ToCoin()
        {
            Coin coin = RedeemScript == null
                ? new Coin(new OutPoint(TransactionId, Index), new TxOut(Value, ScriptPubKey))
                : new ScriptCoin(new OutPoint(TransactionId, Index), new TxOut(Value, ScriptPubKey), RedeemScript);
            if (AssetId != null)
                return coin.ToColoredCoin(new AssetMoney(AssetId, Quantity));
            return coin;
        }
    }
}