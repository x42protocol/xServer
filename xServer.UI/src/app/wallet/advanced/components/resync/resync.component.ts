import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import { WalletInfo } from '../../../../shared/models/wallet-info';
import { ApiService } from '../../../../shared/services/fullnode.api.service';
import { GlobalService } from '../../../../shared/services/global.service';
import { ModalService } from '../../../../shared/services/modal.service';
import { WalletRescan } from '../../../../shared/models/wallet-rescan';

@Component({
  selector: 'app-resync',
  templateUrl: './resync.component.html',
  styleUrls: ['./resync.component.css']
})
export class ResyncComponent implements OnInit, OnDestroy {

  constructor(
    private globalService: GlobalService,
    private apiService: ApiService,
    private genericModalService: ModalService,
    private fb: FormBuilder,
  ) { }
  private walletName: string;
  private lastBlockSyncedHeight: number;
  private chainTip: number;
  private isChainSynced: boolean;
  private generalWalletInfoSubscription: Subscription;

  public isSyncing = true;
  public minDate = new Date('2009-08-09');
  public maxDate = new Date();
  public rescanWalletForm: FormGroup;

  formErrors = {
    walletDate: ''
  };

  validationMessages = {
    walletDate: {
      required: 'Please choose the date the wallet should sync from.'
    }
  };

  ngOnInit() {
    this.walletName = this.globalService.getWalletName();
    this.startSubscriptions();
    this.buildRescanWalletForm();
  }

  ngOnDestroy() {
    this.cancelSubscriptions();
  }

  private buildRescanWalletForm(): void {
    this.rescanWalletForm = this.fb.group({
      walletDate: ['', Validators.required],
    });

    this.rescanWalletForm.valueChanges
      .subscribe(data => this.onValueChanged(data));

    this.onValueChanged();
  }

  onValueChanged(data?: any) {
    if (!this.rescanWalletForm) { return; }
    const form = this.rescanWalletForm;

    // tslint:disable-next-line:forin
    for (const field in this.formErrors) {
      this.formErrors[field] = '';
      const control = form.get(field);
      if (control && control.dirty && !control.valid) {
        const messages = this.validationMessages[field];

        // tslint:disable-next-line:forin
        for (const key in control.errors) {
          this.formErrors[field] += messages[key] + ' ';
        }
      }
    }
  }

  public onResyncClicked() {
    const rescanDate = new Date(this.rescanWalletForm.get('walletDate').value);
    rescanDate.setDate(rescanDate.getDate() - 1);

    const rescanData = new WalletRescan(
      this.walletName,
      rescanDate,
      false,
      true
    );
    this.apiService
      .rescanWallet(rescanData)
      .subscribe(
        response => {
          this.genericModalService.openModal('Rescanning', 'Your wallet is now rescanning. The time remaining depends on the size and creation time of your wallet. The wallet dashboard shows your progress.');
        }
      );
  }

  private getGeneralWalletInfo() {
    const walletInfo = new WalletInfo(this.walletName);
    this.generalWalletInfoSubscription = this.apiService.getGeneralInfo(walletInfo)
      .subscribe(
        response => {
          const generalWalletInfoResponse = response;
          this.lastBlockSyncedHeight = generalWalletInfoResponse.lastBlockSyncedHeight;
          this.chainTip = generalWalletInfoResponse.chainTip;
          this.isChainSynced = generalWalletInfoResponse.isChainSynced;

          if (this.isChainSynced && this.lastBlockSyncedHeight === this.chainTip) {
            this.isSyncing = false;
          } else {
            this.isSyncing = true;
          }
        },
        error => {
          this.cancelSubscriptions();
          this.startSubscriptions();
        }
      )
      ;
  }

  private cancelSubscriptions() {
    if (this.generalWalletInfoSubscription) {
      this.generalWalletInfoSubscription.unsubscribe();
    }
  }

  private startSubscriptions() {
    this.getGeneralWalletInfo();
  }

}
