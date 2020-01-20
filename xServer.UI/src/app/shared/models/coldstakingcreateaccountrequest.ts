export class ColdStakingCreateAccountRequest {
  constructor(
    walletName: string,
    walletPassword: string,
    isColdWalletAccount: boolean
  ) {
    this.walletName = walletName;
    this.walletPassword = walletPassword;
    this.isColdWalletAccount = isColdWalletAccount;
  }

  public walletName: string;
  public walletPassword: string;
  public isColdWalletAccount: boolean;
}
