export class ServerSetupRequest {
  constructor(signAddress: string, keyAddress: string) {
    this.signAddress = signAddress;
    this.keyAddress = keyAddress;
  }

  public signAddress: string;
  public keyAddress: string;
}
