using System.Collections.Generic;

public class BlockModel
{
    public List<TransactionModel> Transactions { get; set; }

    public List<string> Tx { get; set; }

    public string Hash { get; set; }

    public int Confirmations { get; set; }

    public int Size { get; set; }

    public int Weight { get; set; }

    public int Height { get; set; }

    public int Version { get; set; }

    public string VersionHex { get; set; }

    public string Merkleroot { get; set; }

    public int Time { get; set; }

    public int Mediantime { get; set; }

    public int Nonce { get; set; }

    public string Bits { get; set; }

    public double Difficulty { get; set; }

    public string Chainwork { get; set; }

    public int NTx { get; set; }

    public string Previousblockhash { get; set; }

    public string Nextblockhash { get; set; }

    public string Signature { get; set; }

    public string Modifierv2 { get; set; }

    public string Flags { get; set; }

    public string Hashproof { get; set; }

    public string Blocktrust { get; set; }

    public string Chaintrust { get; set; }
}
