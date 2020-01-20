using System;
using System.Reflection;
using NBitcoin;
using Newtonsoft.Json;

namespace X42.Utilities.JsonConverters
{
    /// <summary>
    ///     Converter used to convert an object implementing <see cref="IBitcoinString" /> to and from JSON.
    /// </summary>
    /// <seealso cref="Newtonsoft.Json.JsonConverter" />
    public class BitcoinStringJsonConverter : JsonConverter
    {
        public BitcoinStringJsonConverter(Network network)
        {
            Network = network;
        }

        public Network Network { get; set; }

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return
                typeof(IBitcoinString).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo()) ||
                typeof(IDestination).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo()) &&
                objectType.GetTypeInfo().AssemblyQualifiedName.Contains("NBitcoin");
        }

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            try
            {
                IBitcoinString result = Network.Parse(reader.Value.ToString(), null);
                if (result == null) throw new JsonObjectException("Invalid BitcoinString data", reader);

                if (Network != null)
                    if (result.Network != Network)
                    {
                        result = Network.Parse(reader.Value.ToString());
                        if (result.Network != Network)
                            throw new JsonObjectException("Invalid BitcoinString network", reader);
                    }

                if (!objectType.GetTypeInfo().IsAssignableFrom(result.GetType().GetTypeInfo()))
                    throw new JsonObjectException(
                        "Invalid BitcoinString type expected " + objectType.Name + ", actual " + result.GetType().Name,
                        reader);

                return result;
            }
            catch (FormatException)
            {
                throw new JsonObjectException("Invalid Base58Check data", reader);
            }
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            IBitcoinString base58 = value as IBitcoinString;
            if (base58 != null) writer.WriteValue(value.ToString());
        }
    }
}