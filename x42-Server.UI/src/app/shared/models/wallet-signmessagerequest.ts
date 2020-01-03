export class SignMessageRequest {
  constructor(walletName: string, accountName: string, password: string, externalAddress: string, message: string) {
    this.walletName = walletName;
    this.accountName = accountName;
    this.password = password;
    this.externalAddress = externalAddress;
    this.message = message;
  }

  walletName: string;
  accountName: string;
  password: string;
  externalAddress: string;
  message: string;
}
