import { Component, OnInit, OnDestroy, Input } from '@angular/core';
import { FormGroup, Validators, FormBuilder, AbstractControl } from '@angular/forms';

import { FullNodeApiService } from '../../../shared/services/fullnode.api.service';
import { GlobalService } from '../../../shared/services/global.service';
import { ColdStakingService } from "../../../shared/services/coldstaking.service";

import { DynamicDialogRef, DynamicDialogConfig, DialogService } from 'primeng/api';

import { FeeEstimation } from '../../../shared/models/fee-estimation';
import { TransactionSending } from '../../../shared/models/transaction-sending';
import { WalletInfo } from '../../../shared/models/wallet-info';
import { ColdStakingWithdrawalRequest } from "../../../shared/models/coldstakingwithdrawalrequest";

import { ColdStakingWithdrawConfirmationComponent } from './withdraw-confirmation/withdraw-confirmation.component';

import { Subscription } from 'rxjs';
import { debounceTime } from 'rxjs/operators';


type FeeType = { id: number, display: string, value: number };

@Component({
  selector: 'withdraw-component',
  templateUrl: './withdraw.component.html',
  styleUrls: ['./withdraw.component.css'],
})

export class ColdStakingWithdrawComponent implements OnInit, OnDestroy {
  @Input() address: string;

  feeTypes: FeeType[] = [];
  selectedFeeType: FeeType;

  constructor(private apiService: FullNodeApiService, private globalService: GlobalService, private fb: FormBuilder, private stakingService: ColdStakingService, private dialogService: DialogService, public ref: DynamicDialogRef, public config: DynamicDialogConfig) {
    this.setCoinUnit();
    this.setFeeTypes();
    this.buildSendForm();
  }

  public sendForm: FormGroup;
  public hasOpReturn: boolean;
  public coinUnit: string;
  public isSending: boolean = false;
  public estimatedFee: number = 0;
  public totalBalance: number = 0;
  public spendableBalance: number = 0;
  public apiError: string;
  public firstTitle: string;
  public secondTitle: string;
  public opReturnAmount: number = 0;
  private isColdStaking: boolean;
  private transactionHex: string;
  private walletBalanceSubscription: Subscription;
  private coldStakingAccount: string = "coldStakingColdAddresses";
  private hotStakingAccount: string = "coldStakingHotAddresses";

  ngOnInit() {
    this.isColdStaking = this.config.data.isColdStaking;
    this.startSubscriptions();
  }

  ngOnDestroy() {
    this.cancelSubscriptions();
  };

  private setCoinUnit(): void {
    this.coinUnit = this.globalService.getCoinUnit();
  }

  private getAccount(): string {
    return this.isColdStaking ? this.coldStakingAccount : this.hotStakingAccount;
  }

  private setFeeTypes(): void {
    this.feeTypes.push({ id: 0, display: 'Low - 0.0001 ' + this.coinUnit, value: 0.0001 });
    this.feeTypes.push({ id: 1, display: 'Medium - 0.001 ' + this.coinUnit, value: 0.001 });
    this.feeTypes.push({ id: 2, display: 'High - 0.01 ' + this.coinUnit, value: 0.01 });

    this.selectedFeeType = this.feeTypes[0];
  }

  private buildSendForm(): void {
    this.sendForm = this.fb.group({
      "address": ["", Validators.compose([Validators.required, Validators.minLength(26)])],
      "amount": ["", Validators.compose([Validators.required, Validators.pattern(/^([0-9]+)?(\.[0-9]{0,8})?$/), Validators.min(0.00001), (control: AbstractControl) => Validators.max((this.spendableBalance - this.selectedFeeType.value) / 100000000)(control)])],
      "fee": ["medium", Validators.required],
      "password": ["", Validators.required]
    });

    this.sendForm.valueChanges.pipe(debounceTime(300))
      .subscribe(data => this.onSendValueChanged(data));
  }

  onSendValueChanged(data?: any) {
    if (!this.sendForm) { return; }
    const form = this.sendForm;
    for (const field in this.sendFormErrors) {
      this.sendFormErrors[field] = '';
      const control = form.get(field);
      if (control && control.dirty && !control.valid) {
        const messages = this.sendValidationMessages[field];
        for (const key in control.errors) {
          this.sendFormErrors[field] += messages[key] + ' ';
        }
      }
    }

    this.apiError = "";

    if (this.sendForm.get("address").valid && this.sendForm.get("amount").valid) {
      this.estimateFee();
    }
  }

  sendFormErrors = {
    'address': '',
    'amount': '',
    'fee': '',
    'password': ''
  };

  sendValidationMessages = {
    'address': {
      'required': 'An address is required.',
      'minlength': 'An address is at least 26 characters long.'
    },
    'amount': {
      'required': 'An amount is required.',
      'pattern': 'Enter a valid transaction amount. Only positive numbers and no more than 8 decimals are allowed.',
      'min': "The amount has to be more or equal to 0.00001 XLR.",
      'max': 'The total transaction amount exceeds your spendable balance.'
    },
    'fee': {
      'required': 'A fee is required.'
    },
    'password': {
      'required': 'Your password is required.'
    }
  };

  public getMaxBalance() {

    let maxAmount = this.spendableBalance - (this.selectedFeeType.value * 100000000);
    this.sendForm.patchValue({ amount: maxAmount / 100000000 });
  };

  public estimateFee() {
    let transaction = new FeeEstimation(
      this.globalService.getWalletName(),
      this.getAccount(),
      this.sendForm.get("address").value.trim(),
      this.sendForm.get("amount").value,
      this.sendForm.get("fee").value,
      true
    );

    this.apiService.estimateFee(transaction)
      .subscribe(
        response => {
          this.estimatedFee = response;
        },
        error => {
          this.apiError = error.error.errors[0].message;
        }
      );
  }

  public buildTransaction() {
    this.stakingService.withdrawColdStaking(new ColdStakingWithdrawalRequest(
      this.sendForm.get("address").value.trim(),
      this.sendForm.get("amount").value,
      this.globalService.getWalletName(),
      this.sendForm.get("password").value,
      this.selectedFeeType.value
    )).subscribe(withdrawal => {
      this.transactionHex = withdrawal.transactionHex;
      if (this.isSending) {
        this.hasOpReturn = false;
        this.sendTransaction(this.transactionHex);
      }
    },
      error => {
        this.isSending = false;
        this.apiError = error.error.errors[0].message;
      });
  };

  public send() {
    this.isSending = true;
    this.buildTransaction();
  };

  private sendTransaction(hex: string) {
    let transaction = new TransactionSending(hex);
    this.apiService
      .sendTransaction(transaction)
      .subscribe(
        response => {
          this.ref.close("Close clicked");
          this.openConfirmationModal();
        },
        error => {
          this.isSending = false;
          this.apiError = error.error.errors[0].message;
        }
      );
  }

  private getWalletBalance() {
    let walletInfo = new WalletInfo(this.globalService.getWalletName());
    walletInfo.accountName = this.getAccount();

    this.walletBalanceSubscription = this.apiService.getWalletBalance(walletInfo)
      .subscribe(
        response => {
          let balanceResponse = response;
          this.totalBalance = balanceResponse.balances[0].amountConfirmed + balanceResponse.balances[0].amountUnconfirmed;
          this.spendableBalance = balanceResponse.balances[0].spendableAmount;
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

  private openConfirmationModal() {
    this.dialogService.open(ColdStakingWithdrawConfirmationComponent, {
      header: 'Withdraw Confirmation',
      width: '540px'
    });
  }

  private cancelSubscriptions() {
    if (this.walletBalanceSubscription) {
      this.walletBalanceSubscription.unsubscribe();
    }
  };

  private startSubscriptions() {
    this.getWalletBalance();
  }
}
