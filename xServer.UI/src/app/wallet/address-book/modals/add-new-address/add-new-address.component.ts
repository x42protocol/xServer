import { Component } from '@angular/core';
import { FormGroup, Validators, FormBuilder } from '@angular/forms';
import { DynamicDialogRef } from 'primeng/api';

import { FullNodeApiService } from '../../../../shared/services/fullnode.api.service';
import { AddressLabel } from '../../../../shared/models/address-label';

@Component({
  selector: 'app-add-new-address',
  templateUrl: './add-new-address.component.html',
  styleUrls: ['./add-new-address.component.css']
})
export class AddNewAddressComponent {
  constructor(private FullNodeApiService: FullNodeApiService, private fb: FormBuilder, public ref: DynamicDialogRef) {
    this.buildAddressForm();
  }

  addressForm: FormGroup

  private buildAddressForm(): void {
    this.addressForm = this.fb.group({
      "label": ["", Validators.compose([Validators.required, Validators.minLength(2), Validators.maxLength(40)])],
      "address": ["", Validators.required],
    });

    this.addressForm.valueChanges
      .subscribe(data => this.onValueChanged(data));
  }

  onValueChanged(data?: any) {
    if (!this.addressForm) { return; }
    const form = this.addressForm;
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
    'label': '',
    'address': ''
  };

  validationMessages = {
    'label': {
      'required': 'Please enter a label for your address.',
      'minlength': 'A label needs to be at least 2 characters long.',
      'maxlength': "A label can't be more than 40 characters long."
    },
    'address': {
      'required': 'Please add a valid address.'
    }
  };

  createClicked() {
    let addressLabel = new AddressLabel(this.addressForm.get("label").value, this.addressForm.get("address").value)
    this.FullNodeApiService.addAddressBookAddress(addressLabel)
      .subscribe(
        response => {
          this.ref.close();
        }
      );
  }

  closeClicked() {
    this.ref.close();
  }
}
