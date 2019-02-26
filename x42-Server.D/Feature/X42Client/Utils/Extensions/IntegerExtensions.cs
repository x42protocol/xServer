namespace X42.Feature.X42Client.Utils.Extensions
{
    public static class IntegerExtensions
    {
        /// <summary>
        ///     Checks Whether The Supplied Port Number Is Within A Valid Range
        /// </summary>
        public static bool IsValidPortRange(this uint port)
        {
            return port > 0 && port < 65535;
        }

        /// <summary>
        ///     Checks Whether The Supplied Port Number Is Within A Valid Range
        /// </summary>
        public static bool IsValidPortRange(this ushort port)
        {
            return port > 0 && port < 65535;
        }
    } //emd pf c;ass
}