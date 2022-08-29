import { Component, OnInit, Input, OnDestroy, NgZone } from '@angular/core';
import { FormGroup, FormBuilder } from '@angular/forms';
import { ThemeService } from '../../services/theme.service';
import { SelectItemGroup, MenuItem, SelectItem } from 'primeng/api';
import { DialogService } from 'primeng/dynamicdialog';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { GlobalService } from '../../services/global.service';
import { LogoutConfirmationComponent } from '../../../wallet/logout-confirmation/logout-confirmation.component';
import { ApiService } from '../../../shared/services/fullnode.api.service';
import { WalletInfo } from '../../../shared/models/wallet-info';
import { NodeStatus } from '../../../shared/models/node-status';
import { ApplicationStateService } from '../../services/application-state.service';
import { UpdateService } from '../../services/update.service';
import { Logger } from '../../services/logger.service';

@Component({
  selector: 'app-main-menu',
  templateUrl: './main-menu.component.html',
  styleUrls: ['./main-menu.component.css']
})
export class MainMenuComponent implements OnInit, OnDestroy {
  private updateTimerId: any;

  @Input() public isUnLocked = false;

  private generalWalletInfoSubscription: Subscription;
  private stakingInfoSubscription: Subscription;
  private nodeStatusSubscription: Subscription;
  public lastBlockSyncedHeight: number;
  public chainTip: number;
  public isChainSynced: boolean;
  public connectedNodes = 0;
  private percentSyncedNumber = 0;
  public percentSynced: string;
  public stakingEnabled: boolean;
  public sidechainsEnabled: boolean;
  public settingsMenu: boolean;
  public networks: SelectItem[] = [];
  public isTestnet: boolean;
  public networkForm: FormGroup;
  public changeNetwork: boolean;
  public logoFileName: string;

  groupedThemes: SelectItemGroup[];
  menuItems: MenuItem[];
  toolTip = '';
  connectedNodesTooltip = '';

  constructor(
    private log: Logger,
    private apiService: ApiService,
    private themeService: ThemeService,
    private globalService: GlobalService,
    private router: Router,
    public dialogService: DialogService,
    public appState: ApplicationStateService,
    public updateService: UpdateService,
    private fb: FormBuilder,
    private zone: NgZone,
  ) {

    this.groupedThemes = [
      {
        label: 'Light', value: 'fa fa-lightbulb-o',
        items: [
          { label: 'Green (Default)', value: 'Rhea' },
          { label: 'Alt', value: 'Nova-Alt' },
          { label: 'Accent', value: 'Nova-Accent' }
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


    for (const network of appState.networks) {
      this.networks.push({ label: network.name, value: network.id });
    }

    this.networkForm = this.fb.group({
      selectNetwork: [{ value: appState.network, }],
    });


  }

  setupNodeIPC() {
    throw new Error('Method not implemented.');
  }

  setupXServerIPC() {
    throw new Error('Method not implemented.');

  }

  ngOnInit() {
    this.themeService.setTheme();
    this.themeService.logoFileName.subscribe(x => this.logoFileName = x);
    this.setLogoPath();
    if (this.isUnLocked) {
      this.setUnlockedMenuItems();
    } else {
      this.setDefaultMenuItems();
    }

    setTimeout(() => {
      this.checkForUpdates();
    }, 20000);

    this.updateTimerId = setInterval(() => this.checkForUpdates(), 43200000);
  }

  ngOnDestroy() {
    clearInterval(this.updateTimerId);
    this.cancelSubscriptions();
  }

  applyNetworkChange() {
    this.changeNetwork = false;
    const selectedNetwork = this.networkForm.get('selectNetwork').value;
    console.log(this.appState.network);
    console.log(selectedNetwork);
    if (selectedNetwork !== undefined && this.appState.network !== selectedNetwork) {
      this.appState.updateNetworkSelection(true, 'full', selectedNetwork, this.appState.daemon.path, this.appState.daemon.datafolder);
      console.log('Network Chnaged: ' + selectedNetwork);
      this.changeMode();
    }
  }

  changeMode() {
    this.appState.changingMode = true;

    // Make sure we shut down the existing node when user choose the change mode action.
    this.apiService.shutdownNode().subscribe(response => { });

    this.globalService.setWalletName('');

    this.router.navigateByUrl('');
  }

  checkForUpdates() {
    this.updateService.checkForUpdate();
  }


  setUnlockedMenuItems() {
    this.menuItems = [
      {
        label: 'Dashboard',
        icon: 'pi pi-fw pi-home',
        command: (event: Event) => {
          this.openDashBoard();
        }
      },
      {
        label: 'Server',
        items: [
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

  openAdvanced() {
    this.router.navigate(['/wallet/advanced']);
    this.settingsMenu = false;
  }

  lockClicked() {
    this.dialogService.open(LogoutConfirmationComponent, {
      header: 'Logout'
    });
  }

  private getGeneralWalletInfo() {
    if (this.globalService.getWalletName() === '') {
      this.nodeStatusSubscription = this.apiService.getNodeStatusInterval()
        .subscribe(
          (data: NodeStatus) => {
            const statusResponse = data;
            this.connectedNodes = statusResponse.inboundPeers.length + statusResponse.outboundPeers.length;
            this.lastBlockSyncedHeight = statusResponse.blockStoreHeight;
            if (this.connectedNodes > 0) {
              this.percentSynced = 'Connected...';
            } else {
              this.percentSynced = 'Connecting...';
            }
          },
          error => {
            this.cancelSubscriptions();
            this.startSubscriptions();
          }
        );
    } else {
      const walletInfo = new WalletInfo(this.globalService.getWalletName());
      this.generalWalletInfoSubscription = this.apiService.getGeneralInfo(walletInfo)
        .subscribe(
          response => {
            const generalWalletInfoResponse = response;
            this.lastBlockSyncedHeight = generalWalletInfoResponse.lastBlockSyncedHeight;
            this.chainTip = generalWalletInfoResponse.chainTip;
            this.isChainSynced = generalWalletInfoResponse.isChainSynced;
            this.connectedNodes = generalWalletInfoResponse.connectedNodes;

            const processedText = `Processed ${this.lastBlockSyncedHeight} out of ${this.chainTip} blocks.`;
            this.toolTip = `Synchronizing.  ${processedText}`;

            if (this.connectedNodes === 1) {
              this.connectedNodesTooltip = '1 connection';
            } else if (this.connectedNodes >= 0) {
              this.connectedNodesTooltip = `${this.connectedNodes} connections`;
            }

            if (!this.isChainSynced) {
              this.percentSynced = 'syncing...';
            }
            else {
              this.percentSyncedNumber = ((this.lastBlockSyncedHeight / this.chainTip) * 100);
              if (this.percentSyncedNumber.toFixed(0) === '100' && this.lastBlockSyncedHeight !== this.chainTip) {
                this.percentSyncedNumber = 99;
              }

              this.percentSynced = this.percentSyncedNumber.toFixed(0) + '%';

              if (this.percentSynced === '100%') {
                this.toolTip = `Up to date.  ${processedText}`;
              }
            }
          },
          error => {
            this.cancelSubscriptions();
            this.startSubscriptions();
          }
        );
    }
  }

  private getStakingInfo() {
    this.stakingInfoSubscription = this.apiService.getStakingInfo()
      .subscribe(
        response => {
          const stakingResponse = response;
          this.stakingEnabled = stakingResponse.enabled;
        },
        error => {
          this.cancelSubscriptions();
          this.startSubscriptions();
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

    if (this.nodeStatusSubscription) {
      this.nodeStatusSubscription.unsubscribe();
    }
  }

  private startSubscriptions() {
    this.getGeneralWalletInfo();
    if (!this.sidechainsEnabled) {
      this.getStakingInfo();
    }
  }
}
