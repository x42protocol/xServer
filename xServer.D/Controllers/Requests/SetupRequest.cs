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
        ///     The key address the server will be registered with.
        /// </summary>
        [Required(ErrorMessage = "The key address is required.")]
        public string KeyAddress { get; set; }
    }
}