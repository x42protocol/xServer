using System.ComponentModel.DataAnnotations;

namespace X42.Controllers.Requests
{
    public class SetupRequest
    {
        /// <summary>
        ///     The address the server will be registered with.
        /// </summary>
        [Required(ErrorMessage = "The address is required.")]
        public string Address { get; set; }
    }
}