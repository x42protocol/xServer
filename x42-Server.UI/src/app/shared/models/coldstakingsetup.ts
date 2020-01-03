export class ColdStakingSetup {
  constructor(
      hotWalletAddress: string,
      coldWalletAddress: string,
      amount: number,
      walletName: string,
      walletPassword: string,
      walletAccount: string,
      fees: number
  ) {
    this.hotWalletAddress = hotWalletAddress;
    this.coldWalletAddress = coldWalletAddress;
    this.amount = amount;
    this.walletName = walletName;
    this.walletPassword = walletPassword;
    this.walletAccount = walletAccount;
    this.fees = fees;
  }
  public hotWalletAddress: string;
  public coldWalletAddress: string;
  public amount: number;
  public walletName: string;
  public walletPassword: string;
  public walletAccount: string;
  public fees: number;
}
