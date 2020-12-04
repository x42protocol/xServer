export class WalletInfo {
  constructor(walletName: string) {
    this.walletName = walletName;
  }

  public walletName: string;
  public accountName = 'account 0';
}
