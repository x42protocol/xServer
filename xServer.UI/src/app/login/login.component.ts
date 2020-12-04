import { Component, OnInit } from '@angular/core';
import { FormGroup, Validators, FormBuilder } from '@angular/forms';
import { Router } from '@angular/router';
import { GlobalService } from '../shared/services/global.service';
import { ApiService } from '../shared/services/fullnode.api.service';
import { WalletLoad } from '../shared/models/wallet-load';
import { ThemeService } from '../shared/services/theme.service';
import { ApplicationStateService } from '../shared/services/application-state.service';
import { ServerApiService } from '../shared/services/server.api.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})

export class LoginComponent implements OnInit {
  constructor(
    private globalService: GlobalService,
    private apiService: ApiService,
    private router: Router,
    private fb: FormBuilder,
    public themeService: ThemeService,
    public appState: ApplicationStateService,
    private serverApiService: ServerApiService,
  ) {
    this.buildDecryptForm();
    this.isDarkTheme = themeService.getCurrentTheme().themeType === 'dark';
  }

  public hasWallet = false;
  public isDecrypting = false;
  public isDarkTheme = false;
  public openWalletForm: FormGroup;
  private wallets: [string];

  formErrors = {
    password: ''
  };

  validationMessages = {
    password: {
      required: 'Please enter your password.'
    }
  };

  ngOnInit() {
    this.getWalletFiles();
    this.getCurrentNetwork();
  }

  private buildDecryptForm(): void {
    this.openWalletForm = this.fb.group({
      selectWallet: [{ value: '', disabled: this.isDecrypting }, Validators.required],
      password: [{ value: '', disabled: this.isDecrypting }, Validators.required]
    });

    this.openWalletForm.valueChanges
      .subscribe(data => this.onValueChanged(data));

    this.onValueChanged();
  }

  onValueChanged(data?: any) {
    if (!this.openWalletForm) { return; }
    const form = this.openWalletForm;

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

  private getWalletFiles() {
    this.apiService.getWalletFiles()
      .subscribe(
        response => {
          this.wallets = response.walletsFiles;
          this.globalService.setWalletPath(response.walletsPath);
          if (this.wallets.length > 0) {
            this.hasWallet = true;

            // tslint:disable-next-line:forin
            for (const wallet in this.wallets) {
              this.wallets[wallet] = this.wallets[wallet].slice(0, -12);
            }
          } else {
            this.hasWallet = false;
          }
        }
      );
  }

  public onCreateClicked() {
    this.router.navigate(['setup']);
  }

  public onEnter() {
    this.onDecryptClicked();
  }

  public onDecryptClicked() {
    this.isDecrypting = true;
    this.globalService.setWalletName('x42ServerMain');
    const walletLoad = new WalletLoad(
      'x42ServerMain',
      this.openWalletForm.get('password').value
    );
    this.loadWallet(walletLoad);
  }

  private loadWallet(walletLoad: WalletLoad) {
    this.apiService.loadX42Wallet(walletLoad)
      .subscribe(
        response => {
          this.router.navigate(['wallet/dashboard']);
        },
        error => {
          this.isDecrypting = false;
        }
      );
  }

  private getCurrentNetwork() {
    this.apiService.getNodeStatus()
      .subscribe(
        response => {
          const responseMessage = response;
          this.globalService.setCoinName(responseMessage.coinTicker);
          this.globalService.setCoinUnit(responseMessage.coinTicker);
          this.globalService.setNetwork(responseMessage.network);
          this.appState.fullNodeVersion = responseMessage.version;
          this.appState.protocolVersion = responseMessage.protocolVersion;
          this.appState.dataDirectory = responseMessage.dataDirectoryPath;
        }
      );

    this.serverApiService.getServerStatus()
      .subscribe(
        response => {
          const responseMessage = response;
          this.appState.serverDVersion = responseMessage.version;
        }
      );
  }
}
