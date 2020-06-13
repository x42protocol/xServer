export class ValidateAddressResponse {
  constructor(
    isvalid: boolean,
    address: string,
    scriptPubKey: string,
    isscript: boolean,
    iswitness: boolean
  ) {
    this.isvalid = isvalid;
    this.address = address;
    this.scriptPubKey = scriptPubKey;
    this.isscript = isscript;
    this.iswitness = iswitness;
  }
  public isvalid: boolean;
  public address: string;
  public scriptPubKey: string;
  public isscript: boolean;
  public iswitness: boolean;
}
