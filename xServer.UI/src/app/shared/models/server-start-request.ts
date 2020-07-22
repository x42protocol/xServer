export class ServerStartRequest {
  constructor(walletName: string, password: string, accountName: string, signAddress: string) {
    this.walletName = walletName;
    this.password = password;
    this.accountName = accountName;
    this.signAddress = signAddress;
  }

  public walletName: string;
  public password: string;
  public accountName: string;
  public signAddress: string;
}
