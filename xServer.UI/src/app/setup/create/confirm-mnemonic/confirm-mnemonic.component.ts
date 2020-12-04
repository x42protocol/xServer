import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { FormGroup, Validators, FormBuilder } from '@angular/forms';
import { ApiService } from '../../../shared/services/fullnode.api.service';
import { ColdStakingService } from '../../../shared/services/coldstaking.service';
import { WalletCreation } from '../../../shared/models/wallet-creation';
import { SecretWordIndexGenerator } from './secret-word-index-generator';
import { ThemeService } from '../../../shared/services/theme.service';
import { GlobalService } from '../../../shared/services/global.service';

@Component({
  selector: 'app-confirm-mnemonic',
  templateUrl: './confirm-mnemonic.component.html',
  styleUrls: ['./confirm-mnemonic.component.css']
})
export class ConfirmMnemonicComponent implements OnInit {

  public secretWordIndexGenerator = new SecretWordIndexGenerator();

  constructor(
    private apiService: ApiService,
    private fb: FormBuilder,
    public themeService: ThemeService,
    private stakingService: ColdStakingService,
    private globalService: GlobalService
  ) {
    this.buildMnemonicForm();
    this.isDarkTheme = themeService.getCurrentTheme().themeType === 'dark';
  }

  @Input() queryParams: any;
  @Output() walletCreated: EventEmitter<boolean> = new EventEmitter<boolean>();

  private newWallet: WalletCreation;
  public mnemonicForm: FormGroup;
  public matchError = '';
  public isCreating: boolean;
  public isDarkTheme = false;
  public address: string;

  formErrors = {
    word1: '',
    word2: '',
    word3: ''
  };

  validationMessages = {
    word1: {
      required: 'This secret word is required.',
      minlength: 'A secret word must be at least one character long',
      maxlength: 'A secret word can not be longer than 24 characters',
      pattern: 'Please enter a valid scret word. [a-Z] are the only characters allowed.'
    },
    word2: {
      required: 'This secret word is required.',
      minlength: 'A secret word must be at least one character long',
      maxlength: 'A secret word can not be longer than 24 characters',
      pattern: 'Please enter a valid scret word. [a-Z] are the only characters allowed.'
    },
    word3: {
      required: 'This secret word is required.',
      minlength: 'A secret word must be at least one character long',
      maxlength: 'A secret word can not be longer than 24 characters',
      pattern: 'Please enter a valid scret word. [a-Z] are the only characters allowed.'
    }
  };

  ngOnInit() {
    this.newWallet = new WalletCreation(
      this.queryParams.name,
      this.queryParams.mnemonic,
      this.queryParams.password,
      this.queryParams.passphrase
    );
  }

  private buildMnemonicForm(): void {
    this.mnemonicForm = this.fb.group({
      word1: ['',
        Validators.compose([
          Validators.required,
          Validators.minLength(1),
          Validators.maxLength(24),
          Validators.pattern(/^[a-zA-Z]*$/)
        ])
      ],
      word2: ['',
        Validators.compose([
          Validators.required,
          Validators.minLength(1),
          Validators.maxLength(24),
          Validators.pattern(/^[a-zA-Z]*$/)
        ])
      ],
      word3: ['',
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

    this.matchError = '';
  }

  public onConfirmClicked() {
    this.checkMnemonic();
    if (this.checkMnemonic()) {
      this.isCreating = true;
      this.createWallet(this.newWallet);
    }
  }

  private checkMnemonic(): boolean {
    const mnemonic = this.newWallet.mnemonic;
    const mnemonicArray = mnemonic.split(' ');

    if (this.mnemonicForm.get('word1').value.trim() === mnemonicArray[this.secretWordIndexGenerator.index1] &&
      this.mnemonicForm.get('word2').value.trim() === mnemonicArray[this.secretWordIndexGenerator.index2] &&
      this.mnemonicForm.get('word3').value.trim() === mnemonicArray[this.secretWordIndexGenerator.index3]) {
      return true;
    } else {
      this.matchError = 'The secret words do not match.';
      return false;
    }
  }

  getFirstUnusedAddress() {
    this.stakingService.getAddress(this.globalService.getWalletName(), false, true).subscribe(x => this.address = x.address);
  }

  private createWallet(wallet: WalletCreation) {
    this.apiService.createX42Wallet(wallet)
      .subscribe(
        response => {
          this.stakingService.createColdStakingAccount(wallet.name, wallet.password, true)
            .subscribe(
              createColdStakingAccountResponse => {
                this.stakingService.createColdStakingAccount(wallet.name, wallet.password, false)
                  .subscribe(() => {
                    this.globalService.setWalletName(wallet.name);
                    setTimeout(() => {
                      this.getFirstUnusedAddress();
                    }, 2000);
                    this.walletCreated.emit(true);
                  });
              }
            );
        },
        error => {
          this.isCreating = false;
        }
      );
  }
}
