using System;
using System.Collections.Generic;
using System.Linq;

namespace x42.Feature.PriceLock
{
    public class FiatPair
    {
        /// <summary>
        ///     How many of my prices to hold to average out.
        /// </summary>
        private readonly int myPriceListSize = 10;

        /// <summary>
        ///     How many network prices to hold to average out.
        /// </summary>
        public int NetworkPriceListSize { get; internal set; } = 100;

        /// <summary>
        ///     The currency for this pair.
        /// </summary>
        public FiatCurrency Currency { get; internal set; }

        public PriceList<decimal> MyPrices { get; internal set; }
        private readonly object LOCK_MY_PRICES = new object();

        public PriceList<decimal> NetworkPrices { get; internal set; }
        private readonly object LOCK_NETWORK_PRICES = new object();

        public FiatPair(FiatCurrency currency)
        {
            Currency = currency;
            MyPrices = new PriceList<decimal>(myPriceListSize);
            NetworkPrices = new PriceList<decimal>(NetworkPriceListSize);
        }

        public void AddMyPrice(decimal price)
        {
            lock (LOCK_MY_PRICES)
            {
                MyPrices.Add(price);
            }
        }

        public void AddNetworkPrice(decimal price)
        {
            lock (LOCK_NETWORK_PRICES)
            {
                NetworkPrices.Add(price);
            }
        }

        public decimal GetMytPrice()
        {
            var priceList = new List<decimal>();
            lock (LOCK_MY_PRICES)
            {
                priceList.AddRange(MyPrices);
            }
            return priceList.Average();
        }

        public decimal GetPrice()
        {
            var priceList = new List<decimal>();
            lock (LOCK_MY_PRICES)
            {
                priceList.AddRange(MyPrices);
            }
            lock (LOCK_NETWORK_PRICES)
            {
                priceList.AddRange(NetworkPrices);
            }
            RemoveOutliers(priceList);
            return priceList.Average();
        }

        /// <summary>
        ///     Take out the extremes of the market.
        ///     Will take off the end 10% of the list.
        ///     TODO: Revisit this to perhaps use a % of change from the extremes to remove the outliers instead of this more hard method.
        /// </summary>
        public List<decimal> RemoveOutliers(List<decimal> priceList)
        {
            if (priceList.Count() == 0)
            {
                return new List<decimal>();
            }

            List<decimal> prices = priceList.OrderBy(o => o).ToList();

            var endIndexSize = prices.Count() * 10.0 / 100;
            var ends = Math.Round(endIndexSize, MidpointRounding.AwayFromZero);
            if (ends > 0)
            {
                for (int i = 0; i < ends; i++)
                {
                    prices.RemoveAt(0); // Remove from beginning.
                    prices.RemoveAt(prices.Count() - 1); // Remove from end.
                }
            }

            return prices;
        }

    }
}
