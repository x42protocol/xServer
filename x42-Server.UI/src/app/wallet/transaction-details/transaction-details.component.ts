import { Component, OnInit, OnDestroy } from '@angular/core';
import { DynamicDialogRef, DynamicDialogConfig, SelectItem } from 'primeng/api';
import { Subscription } from 'rxjs';

import { FullNodeApiService } from '../../shared/services/fullnode.api.service';
import { GlobalService } from '../../shared/services/global.service';

import { WalletInfo } from '../../shared/models/wallet-info';
import { TransactionInfo } from '../../shared/models/transaction-info';

@Component({
  selector: 'transaction-details',
  templateUrl: './transaction-details.component.html',
  styleUrls: ['./transaction-details.component.css']
})
export class TransactionDetailsComponent implements OnInit, OnDestroy {
  
  private transaction: TransactionInfo;
  constructor(private FullNodeApiService: FullNodeApiService, private globalService: GlobalService, public ref: DynamicDialogRef, public config: DynamicDialogConfig) {}

  public copied: boolean = false;
  public coinUnit: string;
  public confirmations: number;
  public copyType: SelectItem[];
  private generalWalletInfoSubscription: Subscription;
  private lastBlockSyncedHeight: number;

  ngOnInit() {
    this.copyType = [
      { label: 'Copy', value: 'Copy', icon: 'pi pi-copy' }
    ];

    this.transaction = this.config.data.transaction;
    this.startSubscriptions();
    this.coinUnit = this.globalService.getCoinUnit();
  }

  ngOnDestroy() {
    this.cancelSubscriptions();
  }

  public onCopiedClick() {
    this.copied = true;
  }

  private getGeneralWalletInfo() {
    let walletInfo = new WalletInfo(this.globalService.getWalletName())
    this.generalWalletInfoSubscription = this.FullNodeApiService.getGeneralInfo(walletInfo)
      .subscribe(
        response =>  {
          let generalWalletInfoResponse = response;
          this.lastBlockSyncedHeight = generalWalletInfoResponse.lastBlockSyncedHeight;
          this.getConfirmations(this.transaction);
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
      );
  };

  private getConfirmations(transaction: TransactionInfo) {
    if (transaction.transactionConfirmedInBlock) {
      this.confirmations = this.lastBlockSyncedHeight - Number(transaction.transactionConfirmedInBlock) + 1;
    } else {
      this.confirmations = 0;
    }
  }

  private cancelSubscriptions() {
    if(this.generalWalletInfoSubscription) {
      this.generalWalletInfoSubscription.unsubscribe();
    }
  };

  private startSubscriptions() {
    this.getGeneralWalletInfo();
  }
}
