import { Component, OnInit, OnDestroy, Input } from '@angular/core';
import { FormGroup, Validators, FormBuilder } from '@angular/forms';
import { DialogService } from 'primeng/dynamicdialog';
import { ElectronService } from 'ngx-electron';

import { FullNodeApiService } from '../../shared/services/fullnode.api.service';
import { ServerApiService } from '../../shared/services/server.api.service';
import { GlobalService } from '../../shared/services/global.service';
import { WalletInfo } from '../../shared/models/wallet-info';
import { ThemeService } from '../../shared/services/theme.service';
import { TransactionInfo } from '../../shared/models/transaction-info';

import { SendComponent } from '../send/send.component';
import { ReceiveComponent } from '../receive/receive.component';
import { TransactionDetailsComponent } from '../transaction-details/transaction-details.component';
import { CreateServerIDComponent } from '../server/create-serverid/create-serverid.component';

import { Subscription } from 'rxjs';

import { Router } from '@angular/router';
import { Application } from '../../shared/models/application';

@Component({
  selector: 'dashboard-component',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})

export class DashboardComponent implements OnInit, OnDestroy {
  constructor(private apiService: FullNodeApiService, private serverApiService: ServerApiService, private globalService: GlobalService, public dialogService: DialogService, private router: Router, private fb: FormBuilder, private themeService: ThemeService, private electronService: ElectronService) {
    this.buildStakingForm();
    this.isDarkTheme = themeService.getCurrentTheme().themeType == 'dark';
  }

  public sidechainEnabled: boolean;
  public walletName: string;
  public coinUnit: string;
  public confirmedBalance: number;
  public unconfirmedBalance: number;
  public spendableBalance: number;
  public latestTransactions: TransactionInfo[];
  public hotTransactions: TransactionInfo[];
  public stakingForm: FormGroup;
  public stakingEnabled: boolean;
  public stakingActive: boolean;
  public stakingWeight: number;
  public awaitingMaturity: number = 0;
  public netStakingWeight: number;
  public expectedTime: number;
  public serverSetupStatus: number;
  public dateTime: string;
  public isStarting: boolean;
  public isStopping: boolean;
  public isDarkTheme = false;
  public hasBalance: boolean = false;
  public hotStakingAccount: string = "coldStakingHotAddresses";
  public installedApps: Application[];
  public xServerProfileName: string;

  private walletBalanceSubscription: Subscription;
  private walletHistorySubscription: Subscription;
  private walletHotHistorySubscription: Subscription;
  private stakingInfoSubscription: Subscription;
  private serverSetupStatusSubscription: Subscription;

  ngOnInit() {
    this.sidechainEnabled = this.globalService.getSidechainEnabled();
    this.walletName = this.globalService.getWalletName();
    this.coinUnit = this.globalService.getCoinUnit();
    this.startSubscriptions();

    this.installedApps = [
      { name: 'TestApp', revenue: "231.32423 Tx42" },
      { name: 'Testing', revenue: "0 Tx42" },
    ];
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

  public addApplicationClicked() {

  }

  public manageApp(application: Application) {

  }

  private getWalletBalance() {
    let walletInfo = new WalletInfo(this.globalService.getWalletName());
    walletInfo.accountName = this.hotStakingAccount;

    this.walletBalanceSubscription = this.apiService.getWalletBalance(walletInfo)
      .subscribe(
        balanceResponse => {
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

  private getServerSetupStatus() {
    this.serverSetupStatusSubscription = this.serverApiService.getServerSetupStatusInterval()
      .subscribe(
        response => {
          this.serverSetupStatus = response.serverStatus;
        },
        error => {
          this.cancelSubscriptions();
          this.startSubscriptions();
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

      this.latestTransactions.push(new TransactionInfo(transactionType, transactionId, transactionAmount, transactionFee, transactionConfirmedInBlock, transactionTimestamp));

      if (this.latestTransactions !== undefined && this.latestTransactions.length > 0) {
        if (this.stakingEnabled) {
          this.makeLatestTxListSmall();
        } else {
          this.latestTransactions = this.latestTransactions.slice(0, 5);
        }
      }
    }
  };

  public onOpenServerSetup() {
    this.dialogService.open(CreateServerIDComponent, {
      header: 'Server ID',
      width: '540px'
    });
  }

  private makeLatestTxListSmall() {
    if (this.latestTransactions !== undefined && this.latestTransactions.length > 0) {
      this.latestTransactions = this.latestTransactions.slice(0, 3);
    }
  }

  public startStaking() {
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
      );
  }

  public stopStaking() {
    this.isStopping = true;
    this.isStarting = false;
    this.apiService.stopStaking()
      .subscribe(
        response => {
          this.stakingEnabled = false;
        }
      );
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
      );
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

    if (this.serverSetupStatusSubscription) {
      this.serverSetupStatusSubscription.unsubscribe();
    }
  }

  private startSubscriptions() {
    this.getServerSetupStatus();
    this.getWalletBalance();
    this.getHotHistory();
    if (!this.sidechainEnabled) {
      this.getStakingInfo();
    }
  }
}
