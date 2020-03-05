import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { DialogService } from 'primeng/dynamicdialog';

import { GlobalService } from '../../shared/services/global.service';
import { FullNodeApiService } from '../../shared/services/fullnode.api.service';
import { ColdStakingService } from '../../shared/services/coldstaking.service';
import { ColdStakingCreateAddressComponent } from './create-address/create-address.component';
import { ColdStakingWithdrawComponent } from './withdraw/withdraw.component';
import { ColdStakingCreateComponent } from './create/create.component';
import { ColdStakingCreateHotComponent } from './create-hot/create-hot.component';
import { TransactionDetailsComponent } from '../transaction-details/transaction-details.component';
import { TransactionInfo } from '../../shared/models/transaction-info';
import { WalletInfo } from '../../shared/models/wallet-info';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-staking-scene',
  templateUrl: './overview.component.html',
  styleUrls: ['./overview.component.css']
})
export class ColdStakingOverviewComponent implements OnInit, OnDestroy {

  constructor(private apiService: FullNodeApiService, private globalService: GlobalService, private stakingService: ColdStakingService, private dialogService: DialogService, private fb: FormBuilder) { }

  public coldWalletAccountExists: boolean;
  public hotWalletAccountExists: boolean;

  public coldTransactions: TransactionInfo[];
  public hotTransactions: TransactionInfo[];
  public pageNumber: number = 1;
  public coldStakingAccount: string = "coldStakingColdAddresses";
  public hotStakingAccount: string = "coldStakingHotAddresses";

  public coinUnit: string;

  public confirmedColdBalance: number = 0;
  public confirmedHotBalance: number = 0;

  public unconfirmedColdBalance: number;
  public unconfirmedHotBalance: number;

  public spendableColdBalance: number;
  public spendableHotBalance: number;

  public hasColdBalance: boolean = false;
  public hasHotBalance: boolean = false;

  public lastPrice: number;
  public spendableColdBalanceBaseValue: number;
  public spendableHotBalanceBaseValue: number;

  public isStarting: boolean;
  public isStopping: boolean;
  public stakingEnabled: boolean;
  public stakingActive: boolean;
  public stakingWeight: number;
  public awaitingMaturity: number = 0;
  public netStakingWeight: number;
  public expectedTime: number;
  public dateTime: string;

  private walletColdHistorySubscription: Subscription;
  private walletHotHistorySubscription: Subscription;
  private walletColdBalanceSubscription: Subscription;
  private walletHotBalanceSubscription: Subscription;
  private walletColdWalletExistsSubscription: Subscription;
  private marketSummarySubscription: Subscription;
  private stakingInfoSubscription: Subscription;

  public setupForm: FormGroup;
  private stakingForm: FormGroup;

  ngOnInit() {
    this.buildSetupForm();
    this.buildStakingForm();
    this.coinUnit = this.globalService.getCoinUnit();
    this.startSubscriptions();
  }

  ngOnDestroy() {
    this.cancelSubscriptions();
  }

  private buildSetupForm(): void {
    this.setupForm = this.fb.group({
      "setupType": ["", Validators.compose([Validators.required])]
    });
  }

  private buildStakingForm(): void {
    this.stakingForm = this.fb.group({
      "walletPassword": ["", Validators.required]
    });
  }

  onWalletGetFirstUnusedAddress(isColdStaking: boolean) {
    let modalData = {
      "isColdStaking": isColdStaking
    };

    this.dialogService.open(ColdStakingCreateAddressComponent, {
      header: 'Create Address',
      data: modalData
    });
  }

  onWalletWithdraw(isColdStaking: boolean) {
    let modalData = {
      "isColdStaking": isColdStaking
    };

    this.dialogService.open(ColdStakingWithdrawComponent, {
      header: 'Withdraw',
      data: modalData
    });
  }

  onSetup() {
    this.dialogService.open(ColdStakingCreateComponent, {
      header: 'Setup',
      width: '540px'
    });
  }

  private getColdWalletExists() {
    this.walletColdWalletExistsSubscription = this.stakingService.getInfo(this.globalService.getWalletName()).subscribe(x => {

      var isChanged = (x.coldWalletAccountExists !== this.coldWalletAccountExists || x.hotWalletAccountExists !== this.hotWalletAccountExists);

      if (isChanged)
        this.cancelSubscriptions();

      this.coldWalletAccountExists = x.coldWalletAccountExists;
      this.hotWalletAccountExists = x.hotWalletAccountExists;

      if (isChanged)
        setTimeout(() => {
          this.startSubscriptions();
        }, 2000);
    });
  }

  private getColdHistory() {
    let walletInfo = new WalletInfo(this.globalService.getWalletName());
    walletInfo.accountName = this.coldStakingAccount;

    let historyResponse;
    this.walletColdHistorySubscription = this.apiService.getWalletHistory(walletInfo)
      .subscribe(
        response => {
          if (!!response.history && response.history[0].transactionsHistory.length > 0) {
            historyResponse = response.history[0].transactionsHistory;
            this.getColdTransactionInfo(historyResponse);
          }
        }
      );
  };

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

  private getColdTransactionInfo(transactions: any) {
    this.coldTransactions = [];

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

      this.coldTransactions.push(new TransactionInfo(transactionType, transactionId, transactionAmount, transactionFee, transactionConfirmedInBlock, transactionTimestamp));
    }
  };

  private getHotTransactionInfo(transactions: any) {
    this.hotTransactions = [];

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

      this.hotTransactions.push(new TransactionInfo(transactionType, transactionId, transactionAmount, transactionFee, transactionConfirmedInBlock, transactionTimestamp));
    }
  };

  private getWalletBalance() {
    let walletInfo = new WalletInfo(this.globalService.getWalletName());
    walletInfo.accountName = this.coldStakingAccount;

    this.walletColdBalanceSubscription = this.apiService.getWalletBalance(walletInfo)
      .subscribe(
        coldBalanceResponse => {
          this.confirmedColdBalance = coldBalanceResponse.balances[0].amountConfirmed;
          this.unconfirmedColdBalance = coldBalanceResponse.balances[0].amountUnconfirmed;
          this.spendableColdBalance = coldBalanceResponse.balances[0].spendableAmount;
        }
      );

    walletInfo.accountName = this.hotStakingAccount;
    this.walletHotBalanceSubscription = this.apiService.getWalletBalance(walletInfo)
      .subscribe(
        hotBalanceResponse => {
          this.confirmedHotBalance = hotBalanceResponse.balances[0].amountConfirmed;
          this.unconfirmedHotBalance = hotBalanceResponse.balances[0].amountUnconfirmed;
          this.spendableHotBalance = hotBalanceResponse.balances[0].spendableAmount;
        }
      );
  }

  public openTransactionDetailDialog(transaction: TransactionInfo) {
    let modalData = {
      "transaction": transaction
    };

    this.dialogService.open(TransactionDetailsComponent, {
      header: 'Receive',
      data: modalData
    });
  }

  private cancelSubscriptions() {
    if (this.walletColdHistorySubscription) {
      this.walletColdHistorySubscription.unsubscribe();
    }
    if (this.walletHotHistorySubscription) {
      this.walletHotHistorySubscription.unsubscribe();
    }
    if (this.walletColdBalanceSubscription) {
      this.walletColdBalanceSubscription.unsubscribe();
    }
    if (this.walletHotBalanceSubscription) {
      this.walletHotBalanceSubscription.unsubscribe();
    }
    if (this.walletColdWalletExistsSubscription) {
      this.walletColdWalletExistsSubscription.unsubscribe();
    }
    if (this.marketSummarySubscription) {
      this.marketSummarySubscription.unsubscribe();
    }
    if (this.stakingInfoSubscription) {
      this.stakingInfoSubscription.unsubscribe();
    }
  };

  private startSubscriptions() {
    this.getColdWalletExists();

    if (!this.coldWalletAccountExists)
      return;

    this.getWalletBalance();
    this.getHotHistory();
    this.getColdHistory();
  };

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
          this.awaitingMaturity = (this.unconfirmedHotBalance + this.confirmedHotBalance) - this.spendableHotBalance;
          this.expectedTime = stakingResponse.expectedTime;
          this.dateTime = this.secondsToString(this.expectedTime);
          if (this.stakingActive) {
            this.isStarting = false;
          } else {
            this.isStopping = false;
          }
        }, error => {
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
}
