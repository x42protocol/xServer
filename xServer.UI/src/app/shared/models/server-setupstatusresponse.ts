export class ServerSetupStatusResponse {
  constructor(serverStatus: number) {
    this.serverStatus = serverStatus;
  }

  public serverStatus: number;
}
