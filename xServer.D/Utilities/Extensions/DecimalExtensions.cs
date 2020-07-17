using System;

namespace x42.Utilities.Extensions
{
    /// <summary>
    ///     Provides a set of extension methods for the <see cref="decimal" /> class.
    /// </summary>
    public static class DecimalExtensions
    {
        /// <summary>
        ///     Removes zeros at the end.
        /// </summary>
        /// <param name="value">Any decimal</param>
        /// <returns>The given decimal with out the zeros at the endt</returns>
        public static decimal Normalize(this decimal value)
        {
            return Convert.ToDecimal(string.Format("{0:G29}", decimal.Parse(value.ToString(), System.Globalization.CultureInfo.GetCultureInfo("en-US"))));
        }
    }
}
