export class ServerIDResponse {
  public address: string;
  public serverId: string = '';

  public setServerId(address: string) {
    this.serverId = "SID" + btoa(address);
  }

  public getAddressFromServerId() {
    return atob(this.serverId.substring(3));
  }
}
