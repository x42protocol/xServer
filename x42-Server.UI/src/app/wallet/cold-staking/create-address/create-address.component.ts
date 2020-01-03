import { Component, OnInit } from '@angular/core';
import { DynamicDialogRef, DynamicDialogConfig, SelectItem } from 'primeng/api';

import { ColdStakingService } from '../../../shared/services/coldstaking.service';
import { GlobalService } from '../../../shared/services/global.service';
import { ThemeService } from '../../../shared/services/theme.service';

@Component({
  selector: 'app-create-address',
  templateUrl: './create-address.component.html',
  styleUrls: ['./create-address.component.css']
})
export class ColdStakingCreateAddressComponent implements OnInit {
  constructor(private globalService: GlobalService, private stakingService: ColdStakingService, public ref: DynamicDialogRef, public config: DynamicDialogConfig, private themeService: ThemeService) {
    this.isDarkTheme = themeService.getCurrentTheme().themeType == 'dark';
  }

  public isDarkTheme = false;
  public copyType: SelectItem[];

  address = '';
  addressCopied = false;
  acknowledgeWarning = false;

  private isColdStaking: boolean;

  ngOnInit() {
    this.copyType = [
      { label: 'Copy', value: 'Copy', icon: 'pi pi-copy' }
    ];

    this.isColdStaking = this.config.data.isColdStaking;
    console.log(this.isColdStaking);
    this.stakingService.getAddress(this.globalService.getWalletName(), this.isColdStaking).subscribe(x => this.address = x.address);
  }

  closeClicked() {
    this.ref.close();
  }

  onCopiedClick() {
    this.addressCopied = true;
  }

  onAcknowledge() {
    this.acknowledgeWarning = true;
  }
}
