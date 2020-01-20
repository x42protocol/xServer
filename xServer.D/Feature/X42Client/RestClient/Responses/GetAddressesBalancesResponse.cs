using System.Collections.Generic;

namespace X42.Feature.X42Client.RestClient.Responses
{
    public class GetAddressesBalancesResponse
    {
        public List<Balances> balances { get; set; }
        public string reason { get; set; }
    }
}

public class Balances
{
    public string address { get; set; }
    public long balance { get; set; }
}