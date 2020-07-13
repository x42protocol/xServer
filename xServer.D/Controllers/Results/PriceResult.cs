namespace x42.Controllers.Results
{
    /// <summary>
    ///     Class representing the price information this node has gathered.
    /// </summary>
    public class PriceResult
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PriceResult" /> class.
        /// </summary>
        public PriceResult() { }

        /// <summary>The node's price average.</summary>
        public decimal Price { get; set; } = 0;

        /// <summary>The related pair.</summary>
        public int Pair { get; set; }
    }
}