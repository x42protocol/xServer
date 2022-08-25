using System.Collections.Generic;

namespace Common.Models.Graviex
{
    public class GraviexOrderBookModel
    {
        public int Timestamp { get; set; }
        public List<List<decimal>> Asks { get; set; }
        public List<List<decimal>> Bids { get; set; }

    }
}
