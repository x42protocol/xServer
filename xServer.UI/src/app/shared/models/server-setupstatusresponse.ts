export class ServerSetupStatusResponse {
  constructor(signAddress: string, serverStatus: number, tierLevel: number) {
    this.signAddress = signAddress;
    this.serverStatus = serverStatus;
    this.tierLevel = tierLevel;
  }
  public signAddress: string;
  public serverStatus: number;
  public tierLevel: number;
}
