using System.ComponentModel.DataAnnotations;

namespace Common.Models.XDocuments.DNS
{
    public class NewZoneModel
    {
        [Required]
        public string Kind { get; set; }
        [Required]
        public string Name { get; set; }

        public NewZoneModel(string name)
        {
            Kind = "Native";
            Name = name+".";
        }
    }
}
