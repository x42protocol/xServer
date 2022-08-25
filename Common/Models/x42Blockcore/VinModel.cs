
public class VinModel
{
    public string Coinbase { get; set; }

    public object Sequence { get; set; }

    public string Txid { get; set; }

    public int? Vout { get; set; }

    public ScriptSigModel ScriptSig { get; set; }
}
