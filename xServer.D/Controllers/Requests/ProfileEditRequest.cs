using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace x42.Controllers.Requests
{
    public class ProfileEditRequest
    {
        /// <summary>
        ///     The Public Key Address of the profile requesting to be registered.
        /// </summary>
        [Required(ErrorMessage = "The key address is missing.")]
        [StringLength(128, ErrorMessage = "The key address cannot exceed 128 characters.")]
        public string KeyAddress { get; set; }

        /// <summary>
        ///     Profile fields.
        /// </summary>
        public List<ProfileEditField> ProfileFields { get; set; }
    }
}