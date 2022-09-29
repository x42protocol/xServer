using Newtonsoft.Json;

namespace Common.Models.OrderBook
{
    public class OrderModel
    {
        [JsonProperty("price")]
        public decimal Price { get; set; }
        [JsonProperty("quantity")]
        public decimal Quantity { get; set; }

        public OrderModel(decimal price, decimal quantity)
        {
            Price = price;
            Quantity = quantity;
        }
    }
}
