using System.ComponentModel.DataAnnotations;

namespace x42.Controllers.Requests
{
    public class ProfileRegisterRequest
    {
        /// <summary>
        ///     User defined name of profile to be registered.
        /// </summary>
        [Required(ErrorMessage = "A unique name for the profile is missing")]
        [StringLength(64, ErrorMessage = "The profile name cannot exceed 64 characters.")]
        public string Name { get; set; }

        /// <summary>
        ///     The Public Key Address of the profile requesting to be registered.
        /// </summary>
        [Required(ErrorMessage = "The key address is missing.")]
        [StringLength(128, ErrorMessage = "The key address cannot exceed 128 characters.")]
        public string KeyAddress { get; set; }

        /// <summary>
        ///     The Signature of the profile requesting to be registered.
        /// </summary>
        [Required(ErrorMessage = "The signature is missing.")]
        [StringLength(1024, ErrorMessage = "The signature cannot exceed 1024 characters.")]
        public string Signature { get; set; }

        /// <summary>
        ///     The Price Lock ID of the payment for the registration
        /// </summary>
        [Required(ErrorMessage = "The price lock id is missing.")]
        [StringLength(128, ErrorMessage = "The price lock id cannot exceed 128 characters.")]
        public string PriceLockId { get; set; }
    }
}