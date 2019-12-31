using System.ComponentModel.DataAnnotations;

namespace X42.Controllers.Requests
{
    public class RegisterRequest
    {
        /// <summary>
        ///     Public IP of the server requesting to be registered
        /// </summary>
        [Required(ErrorMessage = "The Ip is missing.")]
        public string Ip { get; set; }

        /// <summary>
        ///     Public Port of the server requesting to be registered
        /// </summary>
        [Required(ErrorMessage = "The Port is missing.")]
        public string Port { get; set; }

        /// <summary>
        ///     The Signature of the server requesting to be registered
        /// </summary>
        [Required(ErrorMessage = "The Signature is missing.")]
        public string Signature { get; set; }

        /// <summary>
        ///     The CollateralTX of the server requesting to be registered
        /// </summary>
        [Required(ErrorMessage = "The CollateralTX is missing.")]
        public string CollateralTX { get; set; }
    }
}