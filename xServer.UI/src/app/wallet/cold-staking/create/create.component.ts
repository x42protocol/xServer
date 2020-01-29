import { Component, OnInit, OnDestroy, Input } from '@angular/core';
import { NgbActiveModal, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { FormGroup, Validators, FormBuilder, AbstractControl } from '@angular/forms';

import { GlobalService } from '../../../shared/services/global.service';
import { FullNodeApiService } from '../../../shared/services/fullnode.api.service';

import { ColdStakingService } from '../../../shared/services/coldstaking.service';
import { ColdStakingCreateSuccessComponent } from '../create-success/create-success.component';
import { TransactionSending } from "../../../shared/models/transaction-sending";
import { Router } from '@angular/router';
import { ColdStakingSetup } from "../../../shared/models/coldstakingsetup";
import { WalletInfo } from '../../../shared/models/wallet-info';
import { Subscription } from 'rxjs';
import { debounceTime } from 'rxjs/operators';


type FeeType = { id: number, display: string, value: number };

@Component({
  selector: 'app-create',
  templateUrl: './create.component.html',
  styleUrls: ['./create.component.css']
})
export class ColdStakingCreateComponent implements OnInit, OnDestroy {
  feeTypes: FeeType[] = [];
  selectedFeeType: FeeType;

  constructor(private apiService: FullNodeApiService, private globalService: GlobalService, private stakingService: ColdStakingService, public activeModal: NgbActiveModal, private modalService: NgbModal, private routerService: Router, private fb: FormBuilder) {
    this.setCoinUnit();
    this.setFeeTypes();
    this.buildSendForm();
  }

  public sendForm: FormGroup;
  public coinUnit: string;
  public spendableBalance: number = 0;
  public maxAmount: number = 0;
  public apiError: string;
  private walletBalanceSubscription: Subscription;
  public totalBalance: number = 0;
  public isSending: boolean = false;

  public ngOnInit() {
    this.startSubscriptions();
  }

  private setCoinUnit(): void {
    this.coinUnit = this.globalService.getCoinUnit();
  }

  private setFeeTypes(): void {
    this.feeTypes.push({ id: 0, display: 'Low - 0.0001 ' + this.coinUnit, value: 0.0001 });
    this.feeTypes.push({ id: 1, display: 'Medium - 0.001 ' + this.coinUnit, value: 0.001 });
    this.feeTypes.push({ id: 2, display: 'High - 0.01 ' + this.coinUnit, value: 0.01 });

    this.selectedFeeType = this.feeTypes[0];
  }

  public getMaxBalance() {
    let maxAmount = this.spendableBalance - (this.selectedFeeType.value * 100000000);
    this.sendForm.patchValue({ amount: maxAmount / 100000000 });
  };

  private buildSendForm(): void {
    this.sendForm = this.fb.group({
      "hotWalletAddress": ["", Validators.compose([Validators.required, Validators.minLength(26)])],
      "amount": ["", Validators.compose([Validators.required, Validators.pattern(/^([0-9]+)?(\.[0-9]{0,8})?$/), Validators.min(0.00001), (control: AbstractControl) => Validators.max((this.spendableBalance - this.selectedFeeType.value) / 100000000)(control)])],
      "password": ["", Validators.required]
    });

    this.sendForm.valueChanges.pipe(debounceTime(300))
      .subscribe(data => this.onSendValueChanged(data));
  }

  private onSendValueChanged(data?: any) {
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
  }

  sendFormErrors = {
    'hotWalletAddress': '',
    'amount': '',
    'password': ''
  };

  sendValidationMessages = {
    'hotWalletAddress': {
      'required': 'An hot wallet address is required.',
      'minlength': 'An hot wallet address is at least 26 characters long.'
    },
    'amount': {
      'required': 'An amount is required.',
      'pattern': 'Enter a valid transaction amount. Only positive numbers and no more than 8 decimals are allowed.',
      'min': "The amount has to be more or equal to 0.00001 XLR.",
      'max': 'The total transaction amount exceeds your spendable balance.'
    },
    'password': {
      'required': 'Your password is required.'
    }
  };

  private getWalletBalance() {
    let walletInfo = new WalletInfo(this.globalService.getWalletName());
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

  private startSubscriptions() {
    this.getWalletBalance();
  }

  private cancelSubscriptions() {
    if (this.walletBalanceSubscription) {
      this.walletBalanceSubscription.unsubscribe();
    }
  };

  public send() {
    this.isSending = true;
    this.buildTransaction();
  };

  public ngOnDestroy() {
    this.cancelSubscriptions();
  };

  public buildTransaction(): void {
    const walletName = this.globalService.getWalletName();
    const walletPassword = this.sendForm.get("password").value;
    const amount = this.sendForm.get("amount").value;
    const hotWalletAddress = this.sendForm.get("hotWalletAddress").value.trim();
    const accountName = "account 0";

    this.stakingService.createColdStakingAccount(walletName, walletPassword, true)
      .subscribe(
        createColdStakingAccountResponse => {
          this.stakingService.getAddress(walletName, true, true).subscribe(getAddressResponse => {
            this.stakingService.createColdstaking(new ColdStakingSetup(
              hotWalletAddress,
              getAddressResponse.address,
              amount,
              walletName,
              walletPassword,
              accountName,
              this.selectedFeeType.value
            ))
              .subscribe(
                createColdstakingResponse => {
                  const transaction = new TransactionSending(createColdstakingResponse.transactionHex);
                  this.apiService
                    .sendTransaction(transaction)
                    .subscribe(
                      sendTransactionResponse => {
                        this.modalService.open(ColdStakingCreateSuccessComponent, { backdrop: 'static' }).result
                          .then(_ => this.activeModal.close());
                      },
                      error => {
                        this.isSending = false;
                        this.apiError = error.error.errors[0].message;
                      }
                    );
                },
                error => {
                  this.isSending = false;
                  this.apiError = error.error.errors[0].message;
                }
              );
          },
            error => {
              this.isSending = false;
              this.apiError = error.error.errors[0].message;
            });
        },
        error => {
          this.isSending = false;
          this.apiError = error.error.errors[0].message;
        }
      );
  }


}
