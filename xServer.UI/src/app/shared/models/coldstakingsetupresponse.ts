export class ColdStakingSetupResponse {
  constructor(
    transactionHex: string
  ) {
    this.transactionHex = transactionHex;
  }
  public transactionHex: string;
}
