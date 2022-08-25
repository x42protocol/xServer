using System.Collections.Generic;

public class ScriptPubKeyModel
{
    public string Asm { get; set; }

    public string Hex { get; set; }

    public string Type { get; set; }

    public int? ReqSigs { get; set; }

    public List<string> Addresses { get; set; }
}
