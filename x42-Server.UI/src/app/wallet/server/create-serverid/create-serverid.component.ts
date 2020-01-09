import { Component, OnInit } from '@angular/core';
import { DynamicDialogRef, DynamicDialogConfig, SelectItem } from 'primeng/api';

import { ColdStakingService } from '../../../shared/services/coldstaking.service';
import { GlobalService } from '../../../shared/services/global.service';
import { ThemeService } from '../../../shared/services/theme.service';

import { ServerIDResponse } from "../../../shared/models/serveridresponse";

@Component({
  selector: 'app-create-serverid',
  templateUrl: './create-serverid.component.html',
  styleUrls: ['./create-serverid.component.css']
})
export class CreateServerIDComponent implements OnInit {
  constructor(private globalService: GlobalService, private stakingService: ColdStakingService, public ref: DynamicDialogRef, public config: DynamicDialogConfig, private themeService: ThemeService) {
    this.isDarkTheme = themeService.getCurrentTheme().themeType == 'dark';
  }

  public isDarkTheme = false;
  public copyType: SelectItem[];

  server: ServerIDResponse = new ServerIDResponse();
  serverIdCopied = false;
  acknowledgeWarning = false;
  ngOnInit() {
    this.copyType = [
      { label: 'Copy', value: 'Copy', icon: 'pi pi-copy' }
    ];

    this.stakingService.getAddress(this.globalService.getWalletName(), false).subscribe(x => this.server.setServerId(x.address));
  }

  closeClicked() {
    this.ref.close();
  }

  onCopiedClick() {
    this.serverIdCopied = true;
  }

  onAcknowledge() {
    this.acknowledgeWarning = true;
  }
}
