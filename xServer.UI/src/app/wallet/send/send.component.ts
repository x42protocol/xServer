import { Component, OnInit, OnDestroy, Input } from '@angular/core';
import { FormGroup, FormControl, Validators, FormBuilder, AbstractControl } from '@angular/forms';

import { FullNodeApiService } from '../../shared/services/fullnode.api.service';
import { GlobalService } from '../../shared/services/global.service';
import { CoinNotationPipe } from '../../shared/pipes/coin-notation.pipe';

import { FeeEstimation } from '../../shared/models/fee-estimation';
import { SidechainFeeEstimation } from '../../shared/models/sidechain-fee-estimation';
import { TransactionBuilding } from '../../shared/models/transaction-building';
import { TransactionSending } from '../../shared/models/transaction-sending';
import { WalletInfo } from '../../shared/models/wallet-info';
import { ThemeService } from '../../shared/services/theme.service';

import { SendConfirmationComponent } from './send-confirmation/send-confirmation.component';

import { Subscription } from 'rxjs';
import { debounceTime } from 'rxjs/operators';

import { DialogService, DynamicDialogRef, DynamicDialogConfig } from 'primeng/api';

@Component({
  selector: 'send-component',
  templateUrl: './send.component.html',
  styleUrls: ['./send.component.css'],
})

export class SendComponent implements OnInit, OnDestroy {
  private address: string;
  constructor(private FullNodeApiService: FullNodeApiService, private globalService: GlobalService, private fb: FormBuilder, public dialogService: DialogService, public ref: DynamicDialogRef, private themeService: ThemeService, public config: DynamicDialogConfig) {
    this.buildSendForm();
    this.buildSendToSidechainForm();
    this.isDarkTheme = themeService.getCurrentTheme().themeType == 'dark';
  }

  public sendForm: FormGroup;
  public sendToSidechainForm: FormGroup;
  public sidechainEnabled: boolean;
  public hasOpReturn: boolean;
  public coinUnit: string;
  public isSending: boolean = false;
  public isDarkTheme = false;
  public estimatedFee: number = 0;
  public estimatedSidechainFee: number = 0;
  public totalBalance: number = 0;
  public apiError: string;
  public firstTitle: string;
  public secondTitle: string;
  public opReturnAmount: number = 1000;
  private transactionHex: string;
  private responseMessage: any;
  private transaction: TransactionBuilding;
  private walletBalanceSubscription: Subscription;

  ngOnInit() {
    this.address = this.config.data.address;

    this.sidechainEnabled = false; //this.globalService.getSidechainEnabled();
    if (this.sidechainEnabled) {
      this.firstTitle = "Sidechain";
      this.secondTitle = "Mainchain";
    } else {
      this.firstTitle = "Mainchain";
      this.secondTitle = "Sidechain";
    }
    this.startSubscriptions();
    this.coinUnit = this.globalService.getCoinUnit();
    if (this.address) {
      this.sendForm.patchValue({ 'address': this.address })
    }
  }

  ngOnDestroy() {
    this.cancelSubscriptions();
  };

  private buildSendForm(): void {
    this.sendForm = this.fb.group({
      "address": ["", Validators.compose([Validators.required, Validators.minLength(26)])],
      "amount": ["", Validators.compose([Validators.required, Validators.pattern(/^([0-9]+)?(\.[0-9]{0,8})?$/), Validators.min(0.00001), (control: AbstractControl) => Validators.max((this.totalBalance - this.estimatedFee) / 100000000)(control)])],
      "fee": ["medium", Validators.required],
      "password": ["", Validators.required]
    });

    this.sendForm.valueChanges.pipe(debounceTime(300))
      .subscribe(data => this.onSendValueChanged(data));
  }

  private buildSendToSidechainForm(): void {
    this.sendToSidechainForm = this.fb.group({
      "federationAddress": ["", Validators.compose([Validators.required, Validators.minLength(26)])],
      "destinationAddress": ["", Validators.compose([Validators.required, Validators.minLength(26)])],
      "amount": ["", Validators.compose([Validators.required, Validators.pattern(/^([0-9]+)?(\.[0-9]{0,8})?$/), Validators.min(0.00001), (control: AbstractControl) => Validators.max((this.totalBalance - this.estimatedFee) / 100000000)(control)])],
      "fee": ["medium", Validators.required],
      "password": ["", Validators.required]
    });

    this.sendToSidechainForm.valueChanges.pipe(debounceTime(300))
      .subscribe(data => this.onSendToSidechainValueChanged(data));
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

  onSendToSidechainValueChanged(data?: any) {
    if (!this.sendToSidechainForm) { return; }
    const form = this.sendToSidechainForm;
    for (const field in this.sendToSidechainFormErrors) {
      this.sendToSidechainFormErrors[field] = '';
      const control = form.get(field);
      if (control && control.dirty && !control.valid) {
        const messages = this.sendToSidechainValidationMessages[field];
        for (const key in control.errors) {
          this.sendToSidechainFormErrors[field] += messages[key] + ' ';
        }
      }
    }

    this.apiError = "";

    if (this.sendToSidechainForm.get("destinationAddress").valid && this.sendToSidechainForm.get("federationAddress").valid && this.sendToSidechainForm.get("amount").valid) {
      this.estimateSidechainFee();
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
      'min': "The amount has to be more or equal to 0.00001 x42.",
      'max': 'The total transaction amount exceeds your available balance.'
    },
    'fee': {
      'required': 'A fee is required.'
    },
    'password': {
      'required': 'Your password is required.'
    }
  };

  sendToSidechainFormErrors = {
    'destionationAddress': '',
    'federationAddress': '',
    'amount': '',
    'fee': '',
    'password': ''
  };

  sendToSidechainValidationMessages = {
    'destinationAddress': {
      'required': 'An address is required.',
      'minlength': 'An address is at least 26 characters long.'
    },
    'federationAddress': {
      'required': 'An address is required.',
      'minlength': 'An address is at least 26 characters long.'
    },
    'amount': {
      'required': 'An amount is required.',
      'pattern': 'Enter a valid transaction amount. Only positive numbers and no more than 8 decimals are allowed.',
      'min': "The amount has to be more or equal to 0.00001 x42.",
      'max': 'The total transaction amount exceeds your available balance.'
    },
    'fee': {
      'required': 'A fee is required.'
    },
    'password': {
      'required': 'Your password is required.'
    }
  };

  public getMaxBalance() {
    let data = {
      walletName: this.globalService.getWalletName(),
      feeType: this.sendForm.get("fee").value
    }

    let balanceResponse;

    this.FullNodeApiService.getMaximumBalance(data)
      .subscribe(
        response => {
          balanceResponse = response;
        },
        error => {
          this.apiError = error.error.errors[0].message;
        },
        () => {
          this.sendForm.patchValue({ amount: +new CoinNotationPipe().transform(balanceResponse.maxSpendableAmount) });
          this.estimatedFee = balanceResponse.fee;
        }
      )
  };

  public estimateFee() {
    let transaction = new FeeEstimation(
      this.globalService.getWalletName(),
      "account 0",
      this.sendForm.get("address").value.trim(),
      this.sendForm.get("amount").value,
      this.sendForm.get("fee").value,
      true
    );

    this.FullNodeApiService.estimateFee(transaction)
      .subscribe(
        response => {
          this.estimatedFee = response;
        },
        error => {
          this.apiError = error.error.errors[0].message;
        }
      );
  }

  public estimateSidechainFee() {
    let sidechainTransaction = new SidechainFeeEstimation(
      this.globalService.getWalletName(),
      "account 0",
      this.sendToSidechainForm.get("federationAddress").value.trim(),
      this.sendToSidechainForm.get("destinationAddress").value.trim(),
      this.sendToSidechainForm.get("amount").value,
      this.sendToSidechainForm.get("fee").value,
      true
    );

    this.FullNodeApiService.estimateSidechainFee(sidechainTransaction)
      .subscribe(
        response => {
          this.estimatedSidechainFee = response;
        },
        error => {
          this.apiError = error.error.errors[0].message;
        }
      );
  }

  public buildTransaction() {
    this.transaction = new TransactionBuilding(
      this.globalService.getWalletName(),
      "account 0",
      this.sendForm.get("password").value,
      this.sendForm.get("address").value.trim(),
      this.sendForm.get("amount").value,
      //this.sendForm.get("fee").value,
      // TO DO: use coin notation
      this.estimatedFee / 100000000,
      true,
      false
    );

    this.FullNodeApiService
      .buildTransaction(this.transaction)
      .subscribe(
        response => {
          this.estimatedFee = response.fee;
          this.transactionHex = response.hex;
          if (this.isSending) {
            this.hasOpReturn = false;
            this.sendTransaction(this.transactionHex);
          }
        },
        error => {
          this.isSending = false;
          this.apiError = error.error.errors[0].message;
        }
      );
  };

  public buildSidechainTransaction() {
    this.transaction = new TransactionBuilding(
      this.globalService.getWalletName(),
      "account 0",
      this.sendToSidechainForm.get("password").value,
      this.sendToSidechainForm.get("federationAddress").value.trim(),
      this.sendToSidechainForm.get("amount").value,
      //this.sendToSidechainForm.get("fee").value,
      // TO DO: use coin notation
      this.estimatedSidechainFee / 100000000,
      true,
      false,
      this.sendToSidechainForm.get("destinationAddress").value.trim(),
      this.opReturnAmount / 100000000
    );

    this.FullNodeApiService.buildTransaction(this.transaction)
      .subscribe(
        response => {
          this.estimatedSidechainFee = response.fee;
          this.transactionHex = response.hex;
          if (this.isSending) {
            this.hasOpReturn = true;
            this.sendTransaction(this.transactionHex);
          }
        },
        error => {
          this.isSending = false;
          this.apiError = error.error.errors[0].message;
        }
      );
  };

  public send() {
    this.isSending = true;
    this.buildTransaction();
  };

  public sendToSidechain() {
    this.isSending = true;
    this.buildSidechainTransaction();
  };


  private sendTransaction(hex: string) {
    let transaction = new TransactionSending(hex);
    this.FullNodeApiService
      .sendTransaction(transaction)
      .subscribe(
        response => {
          this.ref.close("Close clicked");
          this.openConfirmationModal()
        },
        error => {
          this.isSending = false;
          this.apiError = error.error.errors[0].message;
        }
      );
  }

  private getWalletBalance() {
    let walletInfo = new WalletInfo(this.globalService.getWalletName());
    this.walletBalanceSubscription = this.FullNodeApiService.getWalletBalance(walletInfo)
      .subscribe(
        response => {
          let balanceResponse = response;
          //TO DO - add account feature instead of using first entry in array
          this.totalBalance = balanceResponse.balances[0].amountConfirmed + balanceResponse.balances[0].amountUnconfirmed;
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
    let modalData = {
      "transaction": this.transaction,
      "transactionFee": this.estimatedFee,
      "sidechainEnabled": this.sidechainEnabled,
      "opReturnAmount": this.opReturnAmount,
      "hasOpReturn": this.hasOpReturn
    };

    const modalRef = this.dialogService.open(SendConfirmationComponent,
      {
        header: 'Confirmation',
        width: '50%',
        data: modalData
      }
    );
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