import { Component, OnInit } from '@angular/core';
import { ApiService } from '../../../../shared/services/fullnode.api.service';
import { GlobalService } from '../../../../shared/services/global.service';
import { WalletInfo } from '../../../../shared/models/wallet-info';

@Component({
  selector: 'app-ext-pubkey',
  templateUrl: './ext-pubkey.component.html',
  styleUrls: ['./ext-pubkey.component.css']
})
export class ExtPubkeyComponent implements OnInit {
  constructor(
    private apiService: ApiService,
    private globalService: GlobalService,
  ) { }

  public extPubKey: string;
  public copied = false;

  ngOnInit() {
    const walletInfo = new WalletInfo(this.globalService.getWalletName());
    this.getExtPubKey(walletInfo);
  }

  private getExtPubKey(walletInfo: WalletInfo) {
    this.apiService.getExtPubkey(walletInfo)
      .subscribe(
        response => {
          const responseMessage = response;
          this.extPubKey = responseMessage;
        }
      );
  }

  public onCopiedClick() {
    this.copied = true;
  }
}
