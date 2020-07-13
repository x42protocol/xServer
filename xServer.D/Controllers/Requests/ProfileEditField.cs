using System.ComponentModel.DataAnnotations;

namespace x42.Controllers.Requests
{
    public class ProfileEditField
    {
        /// <summary>
        ///     User defined field in profile.
        /// </summary>
        [StringLength(160, ErrorMessage = "The field value cannot exceed 160 characters.")]
        public string Value { get; set; }

        /// <summary>
        ///     The Signature of the field value.
        /// </summary>
        [Required(ErrorMessage = "The Signature is missing.")]
        [StringLength(1024, ErrorMessage = "The Signature cannot exceed 1024 characters.")]
        public string Signature { get; set; }

        /// <summary>
        ///     The Price Lock ID of the payment for the addition of the field.
        /// </summary>
        [Required(ErrorMessage = "The price lock id is missing.")]
        [StringLength(128, ErrorMessage = "The price lock id cannot exceed 128 characters.")]
        public string PriceLockId { get; set; }
    }
}