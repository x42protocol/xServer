import { Component, OnInit, Input, Output, OnDestroy, EventEmitter } from '@angular/core';
import { FormGroup, Validators, FormBuilder } from '@angular/forms';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';

import { GlobalService } from '../../shared/services/global.service';
import { FullNodeApiService } from '../../shared/services/fullnode.api.service';
import { ModalService } from '../../shared/services/modal.service';

import { PasswordValidationDirective } from '../../shared/directives/password-validation.directive';

import { WalletCreation } from '../../shared/models/wallet-creation';
import { WalletInfo } from '../../shared/models/wallet-info';

import { Message } from 'primeng/api';

@Component({
  selector: 'create-component',
  templateUrl: './create.component.html',
  styleUrls: ['./create.component.css'],
})

export class CreateComponent implements OnInit, OnDestroy {
  constructor(private globalService: GlobalService, private FullNodeApiService: FullNodeApiService, private genericModalService: ModalService, private router: Router, private fb: FormBuilder) {

  }

  @Input() name: string;
  @Input() filename: string;
  
  @Output() isCreated: EventEmitter<boolean> = new EventEmitter<boolean>();

  public createWalletForm: FormGroup;
  private newWallet: WalletCreation;
  private mnemonic: string;
  private lockIfWalletExistsSubscription: Subscription;
  displayMnemonic: boolean = false;
  displayMnemonicConfirm: boolean = false;
  queryParams: object;

  resultMessage: Message[] = [];

  walletLocked: boolean;

  ngOnInit() {
    this.getNewMnemonic();
    this.buildCreateForm();
    this.lockIfWalletExists(this.filename);
  }

  private buildCreateForm(): void {
    this.createWalletForm = this.fb.group({
      "walletName": [this.filename,
      Validators.compose([
        Validators.required,
        Validators.minLength(1),
        Validators.maxLength(24),
        Validators.pattern(/^[a-zA-Z0-9]*$/)
      ])
      ],
      "walletPassphrase": [""],
      "walletPassword": ["",
        Validators.required,
        // Validators.compose([
        //   Validators.required,
        //   Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.{10,})/)])
      ],
      "walletPasswordConfirmation": ["", Validators.required],
      "selectNetwork": ["test", Validators.required]
    }, {
        validator: PasswordValidationDirective.MatchPassword
      });

    this.createWalletForm.valueChanges
      .subscribe(data => this.onValueChanged(data));

    this.onValueChanged();
  }

  continueMnemonic() {
    this.displayMnemonic = false;
    this.displayMnemonicConfirm = true;
  }

  walletCreateResult(success: boolean): void {
    if (success) {
      this.walletLocked = true;
      this.resultMessage = [{ severity: 'success', summary: 'Wallet Created', detail: '<br>Your wallet has been created. Keep your secret words, password and passphrase safe!' }];
      this.isCreated.emit(true);
    }
    this.displayMnemonicConfirm = false;
  }

  walletExists(): void {
    this.walletLocked = true;
    this.resultMessage = [{ severity: 'info', summary: 'Wallet Exists', detail: '<br>' + this.name + ' wallet exists, please continue to the next step.' }];
    this.isCreated.emit(true);
  }

  goBackToWords(goBack: boolean): void {
    if (goBack) {
      this.displayMnemonic = true;
      this.displayMnemonicConfirm = false;
    }
  }

  onValueChanged(data?: any) {
    if (!this.createWalletForm) { return; }
    const form = this.createWalletForm;
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
    'walletName': '',
    'walletPassphrase': '',
    'walletPassword': '',
    'walletPasswordConfirmation': ''
  };

  validationMessages = {
    'walletName': {
      'required': 'A wallet name is required.',
      'minlength': 'A wallet name must be at least one character long.',
      'maxlength': 'A wallet name cannot be more than 24 characters long.',
      'pattern': 'Please enter a valid wallet name. [a-Z] and [0-9] are the only characters allowed.'
    },
    'walletPassword': {
      'required': 'A password is required.',
      'pattern': 'A password must be at least 10 characters long and contain one lowercase and uppercase alphabetical character and a number.'
    },
    'walletPasswordConfirmation': {
      'required': 'Confirm your password.',
      'walletPasswordConfirmation': 'Passwords do not match.'
    }
  };

  public onContinueClicked() {
    if (this.mnemonic) {
      this.newWallet = new WalletCreation(
        this.createWalletForm.get("walletName").value,
        this.mnemonic,
        this.createWalletForm.get("walletPassword").value,
        this.createWalletForm.get("walletPassphrase").value,
      );

      this.queryParams = { name: this.newWallet.name, mnemonic: this.newWallet.mnemonic, password: this.newWallet.password, passphrase: this.newWallet.passphrase };
      this.displayMnemonic = true;
    }
  }

  private getNewMnemonic() {
    this.FullNodeApiService.getNewMnemonic()
      .subscribe(
        response => {
          this.mnemonic = response;
        }
      );
  }

  private lockIfWalletExists(walletName: string) {
    let walletInfo = new WalletInfo(walletName)
    this.lockIfWalletExistsSubscription = this.FullNodeApiService.getGeneralInfo(walletInfo, true)
      .subscribe(
        response => {
          this.walletLocked = true;
          this.walletExists();
        },
        error => {
          this.walletLocked = false;
        }
      );
  };

  private cancelSubscriptions() {
    if (this.lockIfWalletExistsSubscription) {
      this.lockIfWalletExistsSubscription.unsubscribe();
    }
  };

  ngOnDestroy() {
    this.cancelSubscriptions();
  }
}
