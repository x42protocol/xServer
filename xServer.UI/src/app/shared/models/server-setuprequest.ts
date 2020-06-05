export class ServerSetupRequest {
  constructor(address: string, keyAddress: string) {
    this.address = address;
    this.keyAddress = keyAddress;
  }

  public address: string;
  public keyAddress: string;
}
