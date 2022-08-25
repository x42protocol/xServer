namespace Common.Models.XServer
{
     public class PingResponse
    {
        public string Version { get; set; }
        public int BestBlockHeight { get; set; }
        public int Tier { get; set; }
    }

}
