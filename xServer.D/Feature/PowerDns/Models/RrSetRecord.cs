namespace x42.Feature.PowerDns.Models
{
    public class RrSetRecord
    {
        public string Content { get; set; }
        public bool Disabled { get; set; }

        public RrSetRecord(string content, bool disabled)
        {
            Content = content;
            Disabled = disabled;
        }
    }
}
