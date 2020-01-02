using System.ComponentModel.DataAnnotations;

namespace X42.Controllers.Requests
{
    public class RegisterRequest
    {
        /// <summary>
        ///     User defined name of server requesting to be registered.
        /// </summary>
        [Required(ErrorMessage = "A name for the server is missing")]
        [StringLength(32, ErrorMessage = "The server node cannot exceed 32 characters.")]
        public string Name { get; set; }

        /// <summary>
        ///     Public IP of the server requesting to be registered.
        /// </summary>
        [Required(ErrorMessage = "The Ip is missing.")]
        [StringLength(45, ErrorMessage = "The Ip cannot exceed 45 characters.")]
        public string Ip { get; set; }

        /// <summary>
        ///     Public Port of the server requesting to be registered.
        /// </summary>
        [Required(ErrorMessage = "The Port is missing.")]
        [Range(1, 65535, ErrorMessage = "The Port cannot be below 1 and not exceed 65535 characters.")]
        public long Port { get; set; }

        /// <summary>
        ///     The Signature of the server requesting to be registered.
        /// </summary>
        [Required(ErrorMessage = "The Signature is missing.")]
        [StringLength(1024, ErrorMessage = "The address cannot exceed 1024 characters.")]
        public string Signature { get; set; }

        /// <summary>
        ///     The Transaction ID of the server requesting to be registered.
        /// </summary>
        [Required(ErrorMessage = "The TxId is missing.")]
        [StringLength(128, ErrorMessage = "The transaction id cannot exceed 128 characters.")]
        public string TxId { get; set; }

        /// <summary>
        ///     The output number in the transaction for the collateral.
        /// </summary>
        [Required(ErrorMessage = "The TxOut is missing.")]
        [Range(0, int.MaxValue, ErrorMessage = "The Port cannot be below 0 and not exceed 2147483647 characters.")]
        public long TxOut { get; set; }
    }
}