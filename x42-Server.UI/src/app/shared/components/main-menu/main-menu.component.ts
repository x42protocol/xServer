import { Component, OnInit, Input, OnDestroy } from '@angular/core';
import { ThemeService } from '../../services/theme.service';
import { SelectItemGroup, MenuItem, DialogService } from 'primeng/api';
import { Router } from '@angular/router';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';

import { Subscription } from 'rxjs';

import { GlobalService } from '../../services/global.service';
import { LogoutConfirmationComponent } from '../../../wallet/logout-confirmation/logout-confirmation.component';
import { FullNodeApiService } from '../../../shared/services/fullnode.api.service';
import { WalletInfo } from '../../../shared/models/wallet-info';

import { SendComponent } from '../../../wallet/send/send.component';
import { ReceiveComponent } from '../../../wallet/receive/receive.component';
import { TransactionDetailsComponent } from '../../../wallet/transaction-details/transaction-details.component';
import { TransactionInfo } from '../../../shared/models/transaction-info';

@Component({
  selector: 'main-menu',
  templateUrl: './main-menu.component.html',
  styleUrls: ['./main-menu.component.css']
})
export class MainMenuComponent implements OnInit, OnDestroy {

  @Input() public isUnLocked: boolean = false;

  private generalWalletInfoSubscription: Subscription;
  private stakingInfoSubscription: Subscription;
  public lastBlockSyncedHeight: number;
  public chainTip: number;
  private isChainSynced: boolean;
  public connectedNodes: number = 0;
  private percentSyncedNumber: number = 0;
  public percentSynced: string;
  public stakingEnabled: boolean;
  public sidechainsEnabled: boolean;
  public settingsMenu: boolean;

  toolTip = '';
  connectedNodesTooltip = '';

  constructor(private FullNodeApiService: FullNodeApiService, private themeService: ThemeService, private globalService: GlobalService, private router: Router, private modalService: NgbModal, public dialogService: DialogService) {

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

  public logoFileName: string;

  groupedThemes: SelectItemGroup[];
  menuItems: MenuItem[];

  ngOnInit() {
    this.themeService.setTheme();
    this.setLogoPath();
    if (this.isUnLocked) {
      this.setUnlockedMenuItems()
    } else {
      this.setDefaultMenuItems();
    }

    this.sidechainsEnabled = this.globalService.getSidechainEnabled();
    this.startSubscriptions();
  }

  setUnlockedMenuItems() {
    this.menuItems = [
      {
        label: 'Server',
        items: [
          {
            label: 'Dashboard',
            icon: 'pi pi-fw pi-home',
            command: (event: Event) => {
              this.openDashBoard();
            }
          },
          { separator: true },
          {
            label: 'Lock',
            icon: 'pi pi-fw pi-sign-out',
            command: (event: Event) => { this.lockClicked(); }
          },
          { separator: true },
          {
            label: 'Quit',
            icon: 'pi pi-fw pi-times',
            command: (event: Event) => { this.quit(); }
          }
        ]
      },
      {
        label: 'Wallet',
        items: [
          {
            label: 'Receive',
            icon: 'pi pi-fw pi-arrow-circle-down',
            command: (event: Event) => {
              this.openReceiveDialog();
            }
          },
          {
            label: 'Send',
            icon: 'pi pi-fw pi-arrow-circle-up',
            command: (event: Event) => {
              this.openSendDialog();
            }
          },
          { separator: true },
          {
            label: 'Address Book',
            icon: 'pi pi-fw pi-list',
            command: (event: Event) => {
              this.openAddressBook();
            }
          },
          {
            label: 'History',
            icon: 'pi pi-fw pi-clock',
            command: (event: Event) => { this.openHistory(); }
          }
        ]
      }
    ];
  }

  setDefaultMenuItems() {
    this.menuItems = [
      {
        label: 'Server',
        items: [
          {
            label: 'Quit',
            icon: 'pi pi-fw pi-times',
            command: (event: Event) => { this.quit(); }
          }
        ]
      }
    ];
  }

  onThemeChange(event) {
    this.themeService.setTheme(event.value);
    this.setLogoPath();
  }

  setLogoPath() {
    this.logoFileName = this.themeService.getLogo();
  }

  quit() {
    this.globalService.quitApplication();
  }

  openDashBoard() {
    this.router.navigate(['/wallet/dashboard']);
  }

  openAddressBook() {
    this.router.navigate(['/wallet/address-book']);
  }

  openHistory() {
    this.router.navigate(['/wallet/history']);
  }

  public openSendDialog() {
    this.dialogService.open(SendComponent, {
      header: 'Send to',
      width: '700px'
    });
  };

  public openReceiveDialog() {
    this.dialogService.open(ReceiveComponent, {
      header: 'Receive',
      width: '540px'
    });
  };
  
  openAdvanced() {
    this.router.navigate(['/wallet/advanced']);
    this.settingsMenu = false;
  }

  lockClicked() {
    this.dialogService.open(LogoutConfirmationComponent, {
      header: 'Logout'
    });
  }

  ngOnDestroy() {
    this.cancelSubscriptions();
  }

  private getGeneralWalletInfo() {
    let walletInfo = new WalletInfo(this.globalService.getWalletName())
    this.generalWalletInfoSubscription = this.FullNodeApiService.getGeneralInfo(walletInfo)
      .subscribe(
        response => {
          let generalWalletInfoResponse = response;
          this.lastBlockSyncedHeight = generalWalletInfoResponse.lastBlockSyncedHeight;
          this.chainTip = generalWalletInfoResponse.chainTip;
          this.isChainSynced = generalWalletInfoResponse.isChainSynced;
          this.connectedNodes = generalWalletInfoResponse.connectedNodes;

          const processedText = `Processed ${this.lastBlockSyncedHeight} out of ${this.chainTip} blocks.`;
          this.toolTip = `Synchronizing.  ${processedText}`;

          if (this.connectedNodes == 1) {
            this.connectedNodesTooltip = "1 connection";
          } else if (this.connectedNodes >= 0) {
            this.connectedNodesTooltip = `${this.connectedNodes} connections`;
          }

          if (!this.isChainSynced) {
            this.percentSynced = "syncing...";
          }
          else {
            this.percentSyncedNumber = ((this.lastBlockSyncedHeight / this.chainTip) * 100);
            if (this.percentSyncedNumber.toFixed(0) === "100" && this.lastBlockSyncedHeight != this.chainTip) {
              this.percentSyncedNumber = 99;
            }

            this.percentSynced = this.percentSyncedNumber.toFixed(0) + '%';

            if (this.percentSynced === '100%') {
              this.toolTip = `Up to date.  ${processedText}`;
            }
          }
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
      )
      ;
  };

  private getStakingInfo() {
    this.stakingInfoSubscription = this.FullNodeApiService.getStakingInfo()
      .subscribe(
        response => {
          let stakingResponse = response
          this.stakingEnabled = stakingResponse.enabled;
        }, error => {
          if (error.status === 0) {
            this.cancelSubscriptions();
          } else if (error.status >= 400) {
            if (!error.error.errors[0].message) {
              this.cancelSubscriptions();
              this.startSubscriptions();
            }
          }
        }
      )
      ;
  }

  private cancelSubscriptions() {
    if (this.generalWalletInfoSubscription) {
      this.generalWalletInfoSubscription.unsubscribe();
    }

    if (this.stakingInfoSubscription) {
      this.stakingInfoSubscription.unsubscribe();
    }
  };

  private startSubscriptions() {
    this.getGeneralWalletInfo();
    if (!this.sidechainsEnabled) {
      this.getStakingInfo();
    }
  }
}
