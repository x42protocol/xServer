import { Component, OnInit } from '@angular/core';
import { FormGroup, FormControl, Validators, FormBuilder } from '@angular/forms';

import { FullNodeApiService } from '../../../../shared/services/fullnode.api.service';
import { GlobalService } from '../../../../shared/services/global.service';
import { ModalService } from '../../../../shared/services/modal.service';
import { WalletInfo } from '../../../../shared/models/wallet-info';

import { SelectItem } from 'primeng/api';

@Component({
  selector: 'app-generate-addresses',
  templateUrl: './generate-addresses.component.html',
  styleUrls: ['./generate-addresses.component.css']
})
export class GenerateAddressesComponent implements OnInit {
  constructor(private FullNodeApiService: FullNodeApiService, private globalService: GlobalService, private genericModalService: ModalService, private fb: FormBuilder) {
    this.buildGenerateAddressesForm();
  }

  public generateAddressesForm: FormGroup;
  public addresses: string[];
  public pageNumber: number = 1;
  public copyType: SelectItem[];

  ngOnInit() {
    this.copyType = [
      { label: 'Copy', value: 'Copy', icon: 'pi pi-copy' }
    ];
  }

  private buildGenerateAddressesForm() {
    this.generateAddressesForm = this.fb.group({
      "generateAddresses": ["", Validators.compose([Validators.required, Validators.pattern("^[0-9]*$"), Validators.min(1), Validators.max(1000)])]
    });

    this.generateAddressesForm.valueChanges
      .subscribe(data => this.onValueChanged(data));

    this.onValueChanged();
  }

  onValueChanged(data?: any) {
    if (!this.generateAddressesForm) { return; }
    const form = this.generateAddressesForm;
    for (const field in this.formErrors) {
      this.formErrors[field] = '';
      const control = form.get(field);
      if (control && control.dirty && !control.valid) {
        const messages = this.validationMessages[field];
        for (const key in control.errors) {
          this.formErrors[field] += messages[key] + ' ';
        }
      }
    }
  }

  formErrors = {
    'generateAddresses': ''
  };

  validationMessages = {
    'generateAddresses': {
      'required': 'Please enter an amount to generate.',
      'pattern': 'Please enter a number between 1 and 10.',
      'min': 'Please generate at least one address.',
      'max': 'You can only generate 1000 addresses at once.'
    }
  };

  public onGenerateClicked() {
    let walletInfo = new WalletInfo(this.globalService.getWalletName());
    this.FullNodeApiService.getUnusedReceiveAddresses(walletInfo, this.generateAddressesForm.get("generateAddresses").value)
      .subscribe(
        response => {
          this.addresses = response;
        }
      );
  }

  public onBackClicked() {
    this.addresses = [''];
  }
}
