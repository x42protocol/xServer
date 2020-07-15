export class ServerSetupResponse {
  constructor(
    signAddress: string
  ) {
    this.signAddress = signAddress;
  }
  public signAddress: string;
}
