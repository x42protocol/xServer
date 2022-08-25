using System.Collections.Generic;

namespace Common.Models.OrderBook
{
    public class OrderBookModel
    {
        public int Timestamp { get; set; }
        public List<OrderModel> Asks { get; set; }
        public List<OrderModel> Bids { get; set; }

        public OrderBookModel(int timstamp)
        {
            Timestamp = timstamp;
            Asks = new List<OrderModel>();
            Bids = new List<OrderModel>();
        }
    }
}
