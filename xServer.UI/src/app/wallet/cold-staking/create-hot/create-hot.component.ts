import { Component } from '@angular/core';
import { DynamicDialogRef, DynamicDialogConfig } from 'primeng/api';
import { FormGroup, Validators, FormBuilder } from '@angular/forms';

import { GlobalService } from '../../../shared/services/global.service';

import { ColdStakingService } from '../../../shared/services/coldstaking.service';

import { debounceTime } from 'rxjs/operators';


type FeeType = { id: number, display: string, value: number };

@Component({
  selector: 'app-create-hot',
  templateUrl: './create-hot.component.html',
  styleUrls: ['./create-hot.component.css']
})
export class ColdStakingCreateHotComponent {
  feeTypes: FeeType[] = [];
  selectedFeeType: FeeType;

  constructor(private globalService: GlobalService, private stakingService: ColdStakingService, private fb: FormBuilder, public ref: DynamicDialogRef, public config: DynamicDialogConfig) {
    this.buildSendForm();
  }

  public sendForm: FormGroup;
  public apiError: string;
  public isSending: boolean = false;
  public address: string;

  private buildSendForm(): void {
    this.sendForm = this.fb.group({
      "password": ["", Validators.required]
    });

    this.sendForm.valueChanges.pipe(debounceTime(300)).subscribe(data => this.onSendValueChanged(data));
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
    'password': ''
  };

  sendValidationMessages = {
    'password': {
      'required': 'Your password is required.'
    }
  };

  getFirstUnusedAddress() {
    this.stakingService.getAddress(this.globalService.getWalletName(), false).subscribe(x => this.address = x.address);
  }

  public createAccount(): void {
    this.isSending = true;
    const walletName = this.globalService.getWalletName();
    const walletPassword = this.sendForm.get("password").value;

    this.stakingService.createColdStakingAccount(walletName, walletPassword, true)
      .subscribe(
        createColdStakingAccountResponse => {
          this.stakingService.createColdStakingAccount(walletName, walletPassword, false)
            .subscribe(() => {
              setTimeout(() => {
                this.getFirstUnusedAddress();
              },
                2000);
            });
        },
        error => {
          this.isSending = false;
          this.apiError = error.error.errors[0].message;
        }
      );
  }


}
