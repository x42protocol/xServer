export class ColdStakingGetInfoResponse {
  constructor(
    coldWalletAccountExists: boolean,
    hotWalletAccountExists: boolean
  ) {
    this.coldWalletAccountExists = coldWalletAccountExists;
    this.hotWalletAccountExists = hotWalletAccountExists;
  }
  public coldWalletAccountExists: boolean;
  public hotWalletAccountExists: boolean;
}
