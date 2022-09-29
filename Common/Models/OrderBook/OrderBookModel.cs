using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Common.Models.OrderBook
{
    public class OrderBookModel
    {
        [BsonElement("timestamp")]
        public int Timestamp { get; set; }
        [BsonElement("asks")]
        [JsonPropertyName("asks")]
        public List<OrderModel> Asks { get; set; }

        [BsonElement("bids")]
        [JsonPropertyName("bids")]
        public List<OrderModel> Bids { get; set; }

        public OrderBookModel(int timstamp)
        {
            Timestamp = timstamp;
            Asks = new List<OrderModel>();
            Bids = new List<OrderModel>();
        }
    }
}
