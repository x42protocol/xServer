import { Component, OnInit } from '@angular/core';
import { FormGroup, Validators, FormBuilder } from '@angular/forms';
import { Router } from '@angular/router';
import { GlobalService } from '../shared/services/global.service';
import { ApiService } from '../shared/services/fullnode.api.service';
import { ServerApiService } from '../shared/services/server.api.service';
import { ServerStatus } from '../shared/models/server-status';
import { WalletLoad } from '../shared/models/wallet-load';
import { ThemeService } from '../shared/services/theme.service';
import { MessageService, Message, SelectItemGroup } from 'primeng/api';

@Component({
  selector: 'app-setup',
  templateUrl: './setup.component.html',
  styleUrls: ['./setup.component.css']
})

export class SetupComponent implements OnInit {
  constructor(
    private globalService: GlobalService,
    private themeService: ThemeService,
    private messageService: MessageService,
    private apiService: ApiService,
    private serverApiService: ServerApiService,
    private router: Router,
    private fb: FormBuilder,
  ) {
    this.buildDecryptForm();

    this.groupedThemes = [
      {
        label: 'Light', value: 'fa fa-lightbulb-o',
        items: [
          { label: 'Green (Default)', value: 'Rhea' },
          { label: 'Blue', value: 'Nova-Dark' },
          { label: 'Mixed Colors', value: 'Nova-Colored' }
        ]
      },
      {
        label: 'Dark', value: 'fa fa-moon-o',
        items: [
          { label: 'Amber', value: 'Luna-amber' },
          { label: 'Blue', value: 'Luna-blue' },
          { label: 'Green', value: 'Luna-green' },
          { label: 'Pink', value: 'Luna-pink' }
        ]
      }
    ];
  }

  public hasWallet = false;
  public isDecrypting = false;
  public selectedTheme: string;
  public logoFileName: string;
  private openWalletForm: FormGroup;
  private wallets: [string];

  walletCreated = false;
  databaseConnected = false;
  msgs: Message[] = [];
  groupedThemes: SelectItemGroup[];
  activeIndex = 0;

  formErrors = {
    password: ''
  };

  validationMessages = {
    password: {
      required: 'Please enter your password.'
    }
  };

  mainWalletCreated(isCreated: boolean): void {
    this.walletCreated = isCreated;
  }

  setLogoPath() {
    this.logoFileName = this.themeService.getLogo();
  }


  onThemeChange(event) {
    this.themeService.setTheme(event.value);
    this.setLogoPath();
  }

  onWizardChange(event) {
    // TODO: Validation here.
  }

  goToStep(stepIndex: number) {
    this.activeIndex = stepIndex;
  }

  next() {
    this.activeIndex++;
  }

  ok() {
    this.activeIndex = 0;
  }

  ngOnInit() {
    this.themeService.setTheme();

    this.setLogoPath();

    this.getWalletFiles();
    this.getCurrentNetwork();

    this.checkDatabase();
    this.messageService.add({ severity: 'success', summary: 'Welcome' });
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
      )
      ;
  }

  public onCreateClicked() {
    this.router.navigate(['setup']);
  }

  public onEnter() {
    if (this.openWalletForm.valid) {
      this.onDecryptClicked();
    }
  }

  public onDecryptClicked() {
    this.isDecrypting = true;
    this.globalService.setWalletName(this.openWalletForm.get('selectWallet').value);
    const walletLoad = new WalletLoad(
      this.openWalletForm.get('selectWallet').value,
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
      )
      ;
  }

  private getCurrentNetwork() {
    this.apiService.getNodeStatus()
      .subscribe(
        response => {
          const responseMessage = response;
          this.globalService.setCoinUnit(responseMessage.coinTicker);
          this.globalService.setNetwork(responseMessage.network);
        }
      );
  }

  private checkDatabase() {
    this.serverApiService.getServerStatus()
      .subscribe(
        (data: ServerStatus) => {
          this.databaseConnected = data.databaseConnected;
        }, (error: any) => {
          console.log('Failed to get database status.');
        }
      );
  }

  public openSetupGuide() {
    throw new Error('Method not implemented.');
  }
}
