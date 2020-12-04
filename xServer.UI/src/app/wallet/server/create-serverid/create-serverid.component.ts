import { Component, OnInit } from '@angular/core';
import { SelectItem } from 'primeng/api';
import { DynamicDialogRef, DynamicDialogConfig } from 'primeng/dynamicdialog';
import { ServerApiService } from '../../../shared/services/server.api.service';
import { ThemeService } from '../../../shared/services/theme.service';
import { ServerIDResponse } from '../../../shared/models/serveridresponse';
import { ServerSetupRequest } from '../../../shared/models/server-setuprequest';
import { ApiService } from '../../../shared/services/fullnode.api.service';

@Component({
  selector: 'app-create-serverid',
  templateUrl: './create-serverid.component.html',
  styleUrls: ['./create-serverid.component.css']
})
export class CreateServerIDComponent implements OnInit {
  constructor(
    private serverApiService: ServerApiService,
    private apiService: ApiService,
    public ref: DynamicDialogRef,
    public config: DynamicDialogConfig,
    public themeService: ThemeService,
  ) {
    this.isDarkTheme = themeService.getCurrentTheme().themeType === 'dark';
    this.elementType = 'url';
  }

  public isDarkTheme = false;
  public copyType: SelectItem[];
  public keyAddress: string;
  public keyAddressAdded: boolean;
  public elementType: string;
  public addressNotValid: boolean;
  public profileNotFound: boolean;
  public keySaving: boolean;

  server: ServerIDResponse = new ServerIDResponse();
  serverIdCopied = false;
  acknowledgeWarning = false;
  ngOnInit() {
    this.copyType = [
      { label: 'Copy', value: 'Copy', icon: 'pi pi-copy' }
    ];
    this.addressNotValid = false;
    this.keySaving = false;
  }

  setKeyAddress() {
    const setup = new ServerSetupRequest('', this.keyAddress);
    this.serverApiService.setSetupAddress(setup).subscribe(
      response => {
        if (response.signAddress === '') {
          this.profileNotFound = true;
          this.keySaving = false;
        } else {
          this.server.setServerId(response.signAddress);
          this.keyAddressAdded = true;
          this.keySaving = false;
        }
      }
    );
  }

  onKeyAddressAdded() {
    this.keySaving = true;
    this.apiService.validateAddress(this.keyAddress, true).subscribe(
      verifyResult => {
        if (verifyResult.isvalid) {
          this.setKeyAddress();
        } else {
          this.addressNotValid = true;
          this.keySaving = false;
        }
      },
      () => {
        this.addressNotValid = true;
        this.keySaving = false;
      });
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
