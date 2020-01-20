using System.Collections.Generic;

namespace X42.Feature.X42Client.RestClient.Responses
{
    public class GetTXOutResponse
    {
        public string bestblock { get; set; }
        public long confirmations { get; set; }
        public long value { get; set; }
        public ScriptPubKey scriptPubKey { get; set; }
    }
}

public class ScriptPubKey
{
    public string asm { get; set; }
    public string hex { get; set; }
    public string reqSigs { get; set; }
    public string type { get; set; }
    public List<string> addresses { get; set; }
}