export class ColdStakingWithdrawalResponse {
  constructor(
    transactionHex: string
  ) {
    this.transactionHex = transactionHex;
  }
  public transactionHex: string;
}
