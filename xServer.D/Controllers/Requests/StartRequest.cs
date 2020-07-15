using System.ComponentModel.DataAnnotations;

namespace x42.Controllers.Requests
{
    /// <summary>
    ///     Request to start the xServer.
    /// </summary>
    public class StartRequest
    {
        /// <summary>
        ///     Wallet name to use for starting the xServer.
        /// </summary>
        [Required(ErrorMessage = "The wallet name is required.")]
        public string WalletName { get; set; }

        /// <summary>
        ///     Wallet password to start xServer with.
        /// </summary>
        [Required(ErrorMessage = "The wallet password is required.")]
        public string Password { get; set; }

        /// <summary>
        ///     Wallet account name.
        /// </summary>
        [Required(ErrorMessage = "The wallet account name is required.")]
        public string AccountName { get; set; }

        /// <summary>
        ///     xServer sign address.
        /// </summary>
        [Required(ErrorMessage = "The sign address is required.")]
        public string SignAddress { get; set; }
    }
}