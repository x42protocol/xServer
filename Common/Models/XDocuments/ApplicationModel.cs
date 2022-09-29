using Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace Common.Models.XDocuments.Applications
{
    public class ApplicationModel : XDocumentModel
    {
        [Required]
        [Range(1, 3,
        ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public ApplicationEnum Application { get; set; }
        public ApplicationModel()
        {

        }
    }
}
