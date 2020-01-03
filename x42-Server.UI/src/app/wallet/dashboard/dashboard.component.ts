import { Component, OnInit, OnDestroy, Input } from '@angular/core';
import { FormGroup, Validators, FormBuilder } from '@angular/forms';
import { DialogService } from 'primeng/api';

import { FullNodeApiService } from '../../shared/services/fullnode.api.service';
import { GlobalService } from '../../shared/services/global.service';
import { WalletInfo } from '../../shared/models/wallet-info';
import { TransactionInfo } from '../../shared/models/transaction-info';
import { ThemeService } from '../../shared/services/theme.service';
import { ColdStakingCreateAddressComponent } from '../cold-staking/create-address/create-address.component';

import { SendComponent } from '../send/send.component';
import { ReceiveComponent } from '../receive/receive.component';
import { TransactionDetailsComponent } from '../transaction-details/transaction-details.component';

import { Subscription } from 'rxjs';

import { Router } from '@angular/router';

@Component({
  selector: 'dashboard-component',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})

export class DashboardComponent implements OnInit, OnDestroy {
  constructor(private apiService: FullNodeApiService, private globalService: GlobalService, public dialogService: DialogService, private router: Router, private fb: FormBuilder, private themeService: ThemeService) {
    this.buildStakingForm();
    this.isDarkTheme = themeService.getCurrentTheme().themeType == 'dark';
  }

  public sidechainEnabled: boolean;
  public walletName: string;
  public coinUnit: string;
  public confirmedBalance: number;
  public unconfirmedBalance: number;
  public spendableBalance: number;
  public transactionArray: TransactionInfo[];
  public latestTransactions: TransactionInfo[];
  public hotTransactions: TransactionInfo[];
  private stakingForm: FormGroup;
  private walletBalanceSubscription: Subscription;
  private walletHistorySubscription: Subscription;
  private walletHotHistorySubscription: Subscription;
  private stakingInfoSubscription: Subscription;
  public stakingEnabled: boolean;
  public stakingActive: boolean;
  public stakingWeight: number;
  public awaitingMaturity: number = 0;
  public netStakingWeight: number;
  public expectedTime: number;
  public dateTime: string;
  public isStarting: boolean;
  public isStopping: boolean;
  public isDarkTheme = false;
  public hasBalance: boolean = false;
  public hotStakingAccount: string = "coldStakingHotAddresses";

  ngOnInit() {
    this.sidechainEnabled = this.globalService.getSidechainEnabled();
    this.walletName = this.globalService.getWalletName();
    this.coinUnit = this.globalService.getCoinUnit();
    this.startSubscriptions();
  };

  ngOnDestroy() {
    this.cancelSubscriptions();
  }

  private buildStakingForm(): void {
    this.stakingForm = this.fb.group({
      "walletPassword": ["", Validators.required]
    });
  }

  public goToHistory() {
    this.router.navigate(['/wallet/history']);
  }

  public openSendDialog() {
    this.dialogService.open(SendComponent, {
      header: 'Send to',
      width: '700px'
    });
  }

  public openReceiveDialog() {
    this.dialogService.open(ReceiveComponent, {
      header: 'Receive',
      width: '540px'
    });
  };

  public openTransactionDetailDialog(transaction: TransactionInfo) {
    let modalData = {
      "transaction": transaction
    };

    this.dialogService.open(TransactionDetailsComponent, {
      header: 'Receive',
      data: modalData
    });
  }

  private getWalletBalance() {
    let walletInfo = new WalletInfo(this.globalService.getWalletName());
    this.walletBalanceSubscription = this.apiService.getWalletBalance(walletInfo)
      .subscribe(
        response => {
          let balanceResponse = response;
          // TO DO - add account feature instead of using first entry in array
          this.confirmedBalance = balanceResponse.balances[0].amountConfirmed;
          this.unconfirmedBalance = balanceResponse.balances[0].amountUnconfirmed;
          this.spendableBalance = balanceResponse.balances[0].spendableAmount;
          if ((this.confirmedBalance + this.unconfirmedBalance) > 0) {
            this.hasBalance = true;
          } else {
            this.hasBalance = false;
          }
        },
        error => {
          this.cancelSubscriptions();
          this.startSubscriptions();
        }
      );

  }

  private getHotHistory() {
    let walletInfo = new WalletInfo(this.globalService.getWalletName());
    walletInfo.accountName = this.hotStakingAccount;

    let historyResponse;
    this.walletHotHistorySubscription = this.apiService.getWalletHistory(walletInfo)
      .subscribe(
        response => {
          if (!!response.history && response.history[0].transactionsHistory.length > 0) {
            historyResponse = response.history[0].transactionsHistory;
            this.getHotTransactionInfo(historyResponse);
          }
        }
      );
  };

  private getHotTransactionInfo(transactions: any) {
    this.latestTransactions = [];

    for (let transaction of transactions) {
      let transactionType;
      if (transaction.type === "send") {
        transactionType = "sent";
      } else if (transaction.type === "received") {
        transactionType = "received";
      } else if (transaction.type === "staked") {
        transactionType = "staked";
      } else {
        transactionType = "unknown";
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

      if (this.stakingEnabled) {
        this.makeLatestTxListSmall();
      } else {
        this.latestTransactions = this.transactionArray.slice(0, 5);
      }

      this.latestTransactions.push(new TransactionInfo(transactionType, transactionId, transactionAmount, transactionFee, transactionConfirmedInBlock, transactionTimestamp));
    }
  };

  onWalletGetFirstUnusedAddress(isColdStaking: boolean) {
    let modalData = {
      "isColdStaking": isColdStaking
    };

    this.dialogService.open(ColdStakingCreateAddressComponent, {
      header: 'xServer Address',
      width: '540px',
      data: modalData
    });
  }

  private makeLatestTxListSmall() {
    this.latestTransactions = this.transactionArray.slice(0, 3);
  }

  private startStaking() {
    this.isStarting = true;
    this.isStopping = false;
    const walletData = {
      name: this.globalService.getWalletName(),
      password: this.stakingForm.get('walletPassword').value
    };
    this.apiService.startStaking(walletData)
      .subscribe(
        response => {
          this.makeLatestTxListSmall();
          this.stakingEnabled = true;
          this.stakingForm.patchValue({ walletPassword: "" });
          this.getStakingInfo();
        },
        error => {
          this.isStarting = false;
          this.stakingEnabled = false;
          this.stakingForm.patchValue({ walletPassword: "" });
        }
      )
      ;
  }

  private stopStaking() {
    this.isStopping = true;
    this.isStarting = false;
    this.apiService.stopStaking()
      .subscribe(
        response => {
          this.stakingEnabled = false;
        }
      )
      ;
  }

  private getStakingInfo() {
    this.stakingInfoSubscription = this.apiService.getStakingInfo()
      .subscribe(
        response => {
          const stakingResponse = response;
          this.stakingEnabled = stakingResponse.enabled;
          this.stakingActive = stakingResponse.staking;
          this.stakingWeight = stakingResponse.weight;
          this.netStakingWeight = stakingResponse.netStakeWeight;
          this.awaitingMaturity = (this.unconfirmedBalance + this.confirmedBalance) - this.spendableBalance;
          this.expectedTime = stakingResponse.expectedTime;
          this.dateTime = this.secondsToString(this.expectedTime);
          if (this.stakingActive) {
            this.makeLatestTxListSmall();
            this.isStarting = false;
          } else {
            this.isStopping = false;
          }
        }, error => {
          this.cancelSubscriptions();
          this.startSubscriptions();
        }
      )
      ;
  }

  private secondsToString(seconds: number) {
    let numDays = Math.floor(seconds / 86400);
    let numHours = Math.floor((seconds % 86400) / 3600);
    let numMinutes = Math.floor(((seconds % 86400) % 3600) / 60);
    let numSeconds = ((seconds % 86400) % 3600) % 60;
    let dateString = "";

    if (numDays > 0) {
      if (numDays > 1) {
        dateString += numDays + " days ";
      } else {
        dateString += numDays + " day ";
      }
    }

    if (numHours > 0) {
      if (numHours > 1) {
        dateString += numHours + " hours ";
      } else {
        dateString += numHours + " hour ";
      }
    }

    if (numMinutes > 0) {
      if (numMinutes > 1) {
        dateString += numMinutes + " minutes ";
      } else {
        dateString += numMinutes + " minute ";
      }
    }

    if (dateString === "") {
      dateString = "Unknown";
    }

    return dateString;
  }

  private cancelSubscriptions() {
    if (this.walletBalanceSubscription) {
      this.walletBalanceSubscription.unsubscribe();
    }

    if (this.walletHistorySubscription) {
      this.walletHistorySubscription.unsubscribe();
    }

    if (this.stakingInfoSubscription) {
      this.stakingInfoSubscription.unsubscribe();
    }

    if (this.walletHotHistorySubscription) {
      this.walletHotHistorySubscription.unsubscribe();
    }
  }

  private startSubscriptions() {
    this.getWalletBalance();
    //this.getHistory();
    this.getHotHistory();
    if (!this.sidechainEnabled) {
      this.getStakingInfo();
    }
  }
}
