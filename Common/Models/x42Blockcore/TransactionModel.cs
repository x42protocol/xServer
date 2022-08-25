using System.Collections.Generic;

public class TransactionModel
{
    public string Hex { get; set; }

    public string Txid { get; set; }

    public string Hash { get; set; }

    public int Version { get; set; }

    public int Size { get; set; }

    public int Vsize { get; set; }

    public int Weight { get; set; }

    public int Locktime { get; set; }

    public List<VinModel> Vin { get; set; }

    public List<VoutModel> Vout { get; set; }
}
