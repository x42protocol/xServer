import { Component, OnInit, OnDestroy, Input } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';

import { WalletCreation } from '../../../shared/models/wallet-creation';

@Component({
  selector: 'app-show-mnemonic',
  templateUrl: './show-mnemonic.component.html',
  styleUrls: ['./show-mnemonic.component.css']
})
export class ShowMnemonicComponent implements OnInit {
  constructor(private route: ActivatedRoute, private router: Router) { }

  @Input() queryParams: any;

  private mnemonic: string;
  private newWallet: WalletCreation;
  public mnemonicArray: string[];
  
  ngOnInit() {
    this.newWallet = new WalletCreation(
      this.queryParams.name,
      this.queryParams.mnemonic,
      this.queryParams.password,
      this.queryParams.passphrase
    )
    this.showMnemonic();
  }

  private showMnemonic() {
    this.mnemonic = this.newWallet.mnemonic;
    if (this.mnemonic) {
      this.mnemonicArray = this.mnemonic.split(" ");
    }
  }

  public onContinueClicked() {
    this.router.navigate(['/setup/create/confirm-mnemonic'], { queryParams: { name: this.newWallet.name, mnemonic: this.newWallet.mnemonic, password: this.newWallet.password, passphrase: this.newWallet.passphrase } });
  }
}
