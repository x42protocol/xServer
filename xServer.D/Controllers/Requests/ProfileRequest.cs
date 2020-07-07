using System.ComponentModel.DataAnnotations;

namespace x42.Controllers.Requests
{
    public class ProfileRequest
    {
        /// <summary>
        ///     The key address of the profile being requested.
        /// </summary>
        [StringLength(128, ErrorMessage = "The key address cannot exceed 128 characters.")]
        public string KeyAddress { get; set; }

        /// <summary>
        ///     The profile name of the profile being requested.
        /// </summary>
        [StringLength(64, ErrorMessage = "The profile name cannot exceed 64 characters.")]
        public string Name { get; set; }
    }
}