namespace Common.Models.x42Blockcore
{
    public class Node
    {
        public string Name { get; set; }
        public int NetworkProtocol { get; set; }
        public string NetworkAddress { get; set; }
        public int Priority { get; set; }
        public int NetworkPort { get; set; }
        public string Version { get; set; }
        public int ResponseTime { get; set; }
        public int Tier { get; set; }
    }
}
