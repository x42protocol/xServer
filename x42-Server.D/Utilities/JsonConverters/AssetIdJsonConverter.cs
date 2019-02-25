using System;
using System.Reflection;
using NBitcoin;
using NBitcoin.OpenAsset;
using Newtonsoft.Json;

namespace X42.Utilities.JsonConverters
{
    /// <summary>
    ///     Converter used to convert an <see cref="AssetId" /> to and from JSON.
    /// </summary>
    /// <seealso cref="Newtonsoft.Json.JsonConverter" />
    public class AssetIdJsonConverter : JsonConverter
    {
        public AssetIdJsonConverter(Network network)
        {
            Guard.NotNull(network, nameof(network));

            Network = network;
        }

        public Network Network { get; }

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return typeof(AssetId).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            try
            {
                string value = reader.Value.ToString();
                return new BitcoinAssetId(value, Network).AssetId;
            }
            catch (FormatException)
            {
                throw new JsonObjectException("Invalid BitcoinAssetId ", reader);
            }
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            AssetId assetId = value as AssetId;
            if (assetId != null) writer.WriteValue(assetId.ToString(Network));
        }
    }
}