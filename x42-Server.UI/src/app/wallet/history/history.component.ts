import { Component, OnInit, OnDestroy } from '@angular/core';
import { DialogService } from 'primeng/api';
import { Router } from '@angular/router';

import { FullNodeApiService } from '../../shared/services/fullnode.api.service';
import { GlobalService } from '../../shared/services/global.service';

import { WalletInfo } from '../../shared/models/wallet-info';
import { TransactionInfo } from '../../shared/models/transaction-info';

import { Subscription } from 'rxjs';

import { TransactionDetailsComponent } from '../transaction-details/transaction-details.component';

@Component({
  selector: 'history-component',
  templateUrl: './history.component.html',
  styleUrls: ['./history.component.css'],
})

export class HistoryComponent {
  constructor(private FullNodeApiService: FullNodeApiService, private globalService: GlobalService, private router: Router, public dialogService: DialogService) {}

  public transactions: TransactionInfo[];
  public coinUnit: string;
  public pageNumber: number = 1;
  public hasTransaction: boolean = true;
  private errorMessage: string;
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

  private openTransactionDetailDialog(transaction: any) {
    let modalData = {
      "transaction": transaction
    };

    this.dialogService.open(TransactionDetailsComponent, {
      header: 'Receive',
      data: modalData
    });
  }

    // todo: add history in seperate service to make it reusable
  private getHistory() {
    let walletInfo = new WalletInfo(this.globalService.getWalletName())
    let historyResponse;
    this.walletHistorySubscription = this.FullNodeApiService.getWalletHistory(walletInfo)
      .subscribe(
        response => {
          //TO DO - add account feature instead of using first entry in array
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
  };

  private getTransactionInfo(transactions: any) {
    this.transactions = [];

    for (let transaction of transactions) {
      let transactionType;
      if (transaction.type === "send") {
        transactionType = "sent";
      } else if (transaction.type === "received") {
        transactionType = "received";
      } else if (transaction.type === "staked") {
        transactionType = "staked";
      }
      let transactionId = transaction.id;
      let transactionAmount = transaction.amount;
      let transactionFee;
      if (transaction.fee) {
        transactionFee = transaction.fee;
      } else {
        transactionFee = 0;
      }
      let transactionConfirmedInBlock = transaction.confirmedInBlock;
      let transactionTimestamp = transaction.timestamp;
      let transactionConfirmed;

      this.transactions.push(new TransactionInfo(transactionType, transactionId, transactionAmount, transactionFee, transactionConfirmedInBlock, transactionTimestamp));
    }
    if (!this.transactions || !this.transactions.length) {
      this.hasTransaction = false;
    }
  }

  private cancelSubscriptions() {
    if(this.walletHistorySubscription) {
      this.walletHistorySubscription.unsubscribe();
    }
  };

  private startSubscriptions() {
    this.getHistory();
  }
}
