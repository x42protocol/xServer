import { Component, OnInit, OnDestroy } from '@angular/core';
import { DialogService } from 'primeng/dynamicdialog';
import { Router } from '@angular/router';
import { ApiService } from '../../shared/services/fullnode.api.service';
import { GlobalService } from '../../shared/services/global.service';
import { ThemeService } from '../../shared/services/theme.service';
import { WalletInfo } from '../../shared/models/wallet-info';
import { TransactionInfo } from '../../shared/models/transaction-info';
import { Subscription } from 'rxjs';
import { TransactionDetailsComponent } from '../transaction-details/transaction-details.component';

@Component({
  selector: 'app-history-component',
  templateUrl: './history.component.html',
  styleUrls: ['./history.component.css'],
})

export class HistoryComponent implements OnInit, OnDestroy {
  constructor(
    private apiService: ApiService,
    private globalService: GlobalService,
    private router: Router,
    public dialogService: DialogService,
    public themeService: ThemeService,
  ) {
    this.isDarkTheme = themeService.getCurrentTheme().themeType === 'dark';
  }

  public transactions: TransactionInfo[];
  public coinUnit: string;
  public pageNumber = 1;
  public hasTransaction = true;
  public isDarkTheme = false;
  public hotStakingAccount = 'coldStakingHotAddresses';

  private walletHistorySubscription: Subscription;

  ngOnInit() {
    this.startSubscriptions();
    this.coinUnit = this.globalService.getCoinUnit();
  }

  ngOnDestroy() {
    this.cancelSubscriptions();
  }

  onDashboardClicked() {
    this.router.navigate(['/wallet']);
  }

  public openTransactionDetailDialog(transaction: any) {
    const modalData = {
      transaction
    };

    this.dialogService.open(TransactionDetailsComponent, {
      header: 'Receive',
      data: modalData
    });
  }

  // todo: add history in seperate service to make it reusable
  private getHistory() {
    const walletInfo = new WalletInfo(this.globalService.getWalletName());
    walletInfo.accountName = this.hotStakingAccount;
    let historyResponse;
    this.walletHistorySubscription = this.apiService.getWalletHistory(walletInfo)
      .subscribe(
        response => {
          // TO DO - add account feature instead of using first entry in array
          if (!!response.history && response.history[0].transactionsHistory.length > 0) {
            historyResponse = response.history[0].transactionsHistory;
            this.getTransactionInfo(historyResponse);
          } else {
            this.hasTransaction = false;
          }
        },
        error => {
          if (error.status === 0) {
            this.cancelSubscriptions();
          } else if (error.status >= 400) {
            if (!error.error.errors[0].message) {
              this.cancelSubscriptions();
              this.startSubscriptions();
            }
          }
        }
      )
      ;
  }

  private getTransactionInfo(transactions: any) {
    this.transactions = [];

    for (const transaction of transactions) {
      let transactionType;
      if (transaction.type === 'send') {
        transactionType = 'sent';
      } else if (transaction.type === 'received') {
        transactionType = 'received';
      } else if (transaction.type === 'staked') {
        transactionType = 'staked';
      }
      const transactionId = transaction.id;
      const transactionAmount = transaction.amount;
      let transactionFee;
      if (transaction.fee) {
        transactionFee = transaction.fee;
      } else {
        transactionFee = 0;
      }
      const transactionConfirmedInBlock = transaction.confirmedInBlock;
      const transactionTimestamp = transaction.timestamp;
      this.transactions.push(new TransactionInfo(transactionType, transactionId, transactionAmount, transactionFee, transactionConfirmedInBlock, transactionTimestamp));
    }
    if (!this.transactions || !this.transactions.length) {
      this.hasTransaction = false;
    }
  }

  private cancelSubscriptions() {
    if (this.walletHistorySubscription) {
      this.walletHistorySubscription.unsubscribe();
    }
  }

  private startSubscriptions() {
    this.getHistory();
  }
}
