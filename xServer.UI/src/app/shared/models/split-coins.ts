export class SplitCoins {
    constructor(walletName: string, accountName: string, walletPassword: string, totalAmountToSplit: string, utxosCount: string) {
        this.walletName = walletName;
        this.accountName = accountName;
        this.walletPassword = walletPassword;
        this.totalAmountToSplit = totalAmountToSplit;
        this.utxosCount = utxosCount;
    }

    walletName: string;
    accountName: string;
    walletPassword: string;
    totalAmountToSplit: string;
    utxosCount: string;
}
