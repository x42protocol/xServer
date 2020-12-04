import { Component, OnInit, OnDestroy, Input } from '@angular/core';
import { Subscription } from 'rxjs';
import { ApiService } from '../../shared/services/fullnode.api.service';
import { GlobalService } from '../../shared/services/global.service';
import { NodeStatus } from '../../shared/models/node-status';
import { XServerStatus } from '../../shared/models/xserver-status';
import { WalletInfo } from '../../shared/models/wallet-info';

@Component({
  selector: 'app-status-bar',
  templateUrl: './status-bar.component.html',
  styleUrls: ['./status-bar.component.css']
})
export class StatusBarComponent implements OnInit, OnDestroy {

  private generalWalletInfoSubscription: Subscription;
  private stakingInfoSubscription: Subscription;
  private nodeStatusSubscription: Subscription;
  private xServerStatusSubscription: Subscription;
  private connectedNodesTooltip = '';
  private connectedXServerTooltip = '';
  private isChainSynced: boolean;
  private percentSyncedNumber = 0;

  public lastBlockSyncedHeight: number;
  public chainTip: number;
  public connectedNodes = 0;
  public connectedXServers = 0;
  public percentSynced: string;
  public stakingEnabled: boolean;
  public connectionsTooltip = '';
  public connectedTooltip = '';
  public toolTip = '';

  @Input() public isUnLocked = false;

  constructor(
    private apiService: ApiService,
    private globalService: GlobalService,
  ) { }

  ngOnInit() {
    this.setDefaultConnectionToolTip();
    this.startSubscriptions();
  }

  ngOnDestroy() {
    this.cancelSubscriptions();
  }

  private updateConnectionToolTip() {
    this.connectionsTooltip = `Connections:\n${this.connectedNodesTooltip}\n${this.connectedXServerTooltip}`;
  }

  private setDefaultConnectionToolTip() {
    this.connectedXServerTooltip = '0 xServers';
    this.connectedNodesTooltip = '0 nodes';
    this.updateConnectionToolTip();
  }

  private getGeneralxServerInfo() {
    this.xServerStatusSubscription = this.apiService.getxServerStatusInterval()
      .subscribe(
        (data: XServerStatus) => {
          const statusResponse = data;
          this.connectedXServers = statusResponse.connected;

          if (statusResponse.connected === 1) {
            this.connectedXServerTooltip = '1 xServer';
          } else if (statusResponse.connected >= 0) {
            this.connectedXServerTooltip = `${statusResponse.connected} xServers`;
          }
          this.updateConnectionToolTip();
        },
        error => {
          this.cancelSubscriptions();
          this.startSubscriptions();
        }
      );
  }

  private getGeneralWalletInfo() {
    if (this.globalService.getWalletName() === '') {
      this.nodeStatusSubscription = this.apiService.getNodeStatusInterval()
        .subscribe(
          (data: NodeStatus) => {
            const statusResponse = data;
            this.connectedNodes = statusResponse.inboundPeers.length + statusResponse.outboundPeers.length;
            this.lastBlockSyncedHeight = statusResponse.blockStoreHeight;
            this.chainTip = statusResponse.bestPeerHeight;

            let processedText = `Processed ${this.lastBlockSyncedHeight} out of ${this.chainTip} blocks.`;
            if (this.chainTip == null) {
              processedText = `Waiting for peer connections to start.`;
            }

            this.toolTip = `Synchronizing.  ${processedText}`;

            if (this.connectedNodes === 1) {
              this.connectedNodesTooltip = '1 node';
            } else if (this.connectedNodes >= 0) {
              this.connectedNodesTooltip = `${this.connectedNodes} nodes`;
            }
            this.updateConnectionToolTip();

            if (this.chainTip == null || this.lastBlockSyncedHeight > this.chainTip) {
              this.percentSynced = 'syncing...';
            } else {
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
              this.connectedNodesTooltip = '1 node';
            } else if (this.connectedNodes >= 0) {
              this.connectedNodesTooltip = `${this.connectedNodes} nodes`;
            }
            this.updateConnectionToolTip();

            if (this.chainTip == null || this.lastBlockSyncedHeight > this.chainTip) {
              this.percentSynced = 'syncing...';
            }
            else {
              this.percentSyncedNumber = ((this.lastBlockSyncedHeight / this.chainTip) * 100);
              if (this.percentSyncedNumber.toFixed(0) === '100' && this.lastBlockSyncedHeight !== this.chainTip && !this.isChainSynced) {
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
        );
    }
  }

  private getStakingInfo() {
    this.stakingInfoSubscription = this.apiService.getStakingInfo()
      .subscribe(
        response => {
          const stakingResponse = response;
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
      );
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

    if (this.xServerStatusSubscription) {
      this.xServerStatusSubscription.unsubscribe();
    }
  }

  private startSubscriptions() {
    this.getGeneralWalletInfo();
    this.getGeneralxServerInfo();
    this.getStakingInfo();
  }
}
