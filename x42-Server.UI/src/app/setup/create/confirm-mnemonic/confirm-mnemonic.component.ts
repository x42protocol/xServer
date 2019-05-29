import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { FormGroup, Validators, FormBuilder } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';

import { FullNodeApiService } from '../../../shared/services/fullnode.api.service';
import { ModalService } from '../../../shared/services/modal.service';

import { WalletCreation } from '../../../shared/models/wallet-creation';
import { SecretWordIndexGenerator } from './secret-word-index-generator';
import { ThemeService } from '../../../shared/services/theme.service';

@Component({
  selector: 'app-confirm-mnemonic',
  templateUrl: './confirm-mnemonic.component.html',
  styleUrls: ['./confirm-mnemonic.component.css']
})
export class ConfirmMnemonicComponent implements OnInit {

  public secretWordIndexGenerator = new SecretWordIndexGenerator();

  constructor(private FullNodeApiService: FullNodeApiService, private genericModalService: ModalService, private route: ActivatedRoute, private router: Router, private fb: FormBuilder, private themeService: ThemeService) {
    this.buildMnemonicForm();
    this.isDarkTheme = themeService.getCurrentTheme().themeType == 'dark';
  }

  @Input() queryParams: any;
  @Output() success: EventEmitter<boolean> = new EventEmitter<boolean>();

  private newWallet: WalletCreation;
  public mnemonicForm: FormGroup;
  public matchError: string = "";
  public isCreating: boolean;
  public isDarkTheme = false;

  ngOnInit() {
    this.newWallet = new WalletCreation(
      this.queryParams.name,
      this.queryParams.mnemonic,
      this.queryParams.password,
      this.queryParams.passphrase
    )
  }

  private buildMnemonicForm(): void {
    this.mnemonicForm = this.fb.group({
      "word1": ["",
        Validators.compose([
          Validators.required,
          Validators.minLength(1),
          Validators.maxLength(24),
          Validators.pattern(/^[a-zA-Z]*$/)
        ])
      ],
      "word2": ["",
        Validators.compose([
          Validators.required,
          Validators.minLength(1),
          Validators.maxLength(24),
          Validators.pattern(/^[a-zA-Z]*$/)
        ])
      ],
      "word3": ["",
        Validators.compose([
          Validators.required,
          Validators.minLength(1),
          Validators.maxLength(24),
          Validators.pattern(/^[a-zA-Z]*$/)
        ])
      ]
    });

    this.mnemonicForm.valueChanges
      .subscribe(data => this.onValueChanged(data));

    this.onValueChanged();
  }

  onValueChanged(data?: any) {
    if (!this.mnemonicForm) { return; }
    const form = this.mnemonicForm;
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

    this.matchError = "";
  }

  formErrors = {
    'word1': '',
    'word2': '',
    'word3': ''
  };

  validationMessages = {
    'word1': {
      'required': 'This secret word is required.',
      'minlength': 'A secret word must be at least one character long',
      'maxlength': 'A secret word can not be longer than 24 characters',
      'pattern': 'Please enter a valid scret word. [a-Z] are the only characters allowed.'
    },
    'word2': {
      'required': 'This secret word is required.',
      'minlength': 'A secret word must be at least one character long',
      'maxlength': 'A secret word can not be longer than 24 characters',
      'pattern': 'Please enter a valid scret word. [a-Z] are the only characters allowed.'
    },
    'word3': {
      'required': 'This secret word is required.',
      'minlength': 'A secret word must be at least one character long',
      'maxlength': 'A secret word can not be longer than 24 characters',
      'pattern': 'Please enter a valid scret word. [a-Z] are the only characters allowed.'
    }
  };

  public onConfirmClicked() {
    this.checkMnemonic();
    if (this.checkMnemonic()) {
      this.isCreating = true;
      this.createWallet(this.newWallet);
    }
  }
  
  private checkMnemonic(): boolean {
    let mnemonic = this.newWallet.mnemonic;
    let mnemonicArray = mnemonic.split(" ");

    if (this.mnemonicForm.get('word1').value.trim() === mnemonicArray[this.secretWordIndexGenerator.index1] &&
        this.mnemonicForm.get('word2').value.trim() === mnemonicArray[this.secretWordIndexGenerator.index2] &&
        this.mnemonicForm.get('word3').value.trim() === mnemonicArray[this.secretWordIndexGenerator.index3]) {
      return true;
    } else {
      this.matchError = 'The secret words do not match.'
      return false;
    }
  }

  private createWallet(wallet: WalletCreation) {
    this.FullNodeApiService.createX42Wallet(wallet)
      .subscribe(
        response => {
          this.success.emit(true);
        },
        error => {
          this.isCreating = false;
        }
      );
  }
}
