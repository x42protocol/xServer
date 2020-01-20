export class ColdStakingWithdrawalRequest {
  constructor(
    receivingAddress: string,
    amount: number,
    walletName: string,
    walletPassword: string,
    fees: number
  ) {
    this.receivingAddress = receivingAddress;
    this.amount = amount;
    this.walletName = walletName;
    this.walletPassword = walletPassword;
    this.fees = fees;
  }
  public receivingAddress: string;
  public amount: number;
  public walletName: string;
  public walletPassword: string;
  public fees: number;
}
