using System.ComponentModel.DataAnnotations;

namespace x42.Controllers.Requests
{
    public class CreatePriceLockRequest
    {
        /// <summary>
        ///     The inital amount to create the price lock on.
        /// </summary>
        [Required(ErrorMessage = "Initial amount is missing")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Value for {0} cannot be below {1} and not exceed {2}.")]
        public decimal InitialAmount { get; set; }

        /// <summary>
        ///     The destination address of the profile requesting to be registered.
        /// </summary>
        [Required(ErrorMessage = "The Destination Address is missing.")]
        [StringLength(128, ErrorMessage = "Value for {0} cannot be below {1} and not exceed {2}.")]
        public string DestinationAddress { get; set; }

        /// <summary>
        ///     Time to watch in blocks before price lock expires.
        /// </summary>
        [Required(ErrorMessage = "ExpireBlock is missing")]
        [Range(1, long.MaxValue, ErrorMessage = "Value for {0} cannot be below {1} and not exceed {2}.")]
        public long ExpireBlock { get; set; }
    }
}