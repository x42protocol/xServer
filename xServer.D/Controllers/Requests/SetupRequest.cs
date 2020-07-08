using System.ComponentModel.DataAnnotations;

namespace x42.Controllers.Requests
{
    public class SetupRequest
    {
        /// <summary>
        ///     The address the server will be registered with.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        ///     The profile namethe server will be registered with.
        /// </summary>
        [Required(ErrorMessage = "The profile name is required.")]
        public string ProfileName { get; set; }
    }
}