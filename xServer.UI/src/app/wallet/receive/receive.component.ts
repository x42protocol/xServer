import { Component, OnInit } from '@angular/core';

import { FullNodeApiService } from '../../shared/services/fullnode.api.service';
import { GlobalService } from '../../shared/services/global.service';
import { ThemeService } from '../../shared/services/theme.service';

import { WalletInfo } from '../../shared/models/wallet-info';

import { SelectItem } from 'primeng/api';
import { DynamicDialogRef } from 'primeng/dynamicdialog';

@Component({
  selector: 'receive-component',
  templateUrl: './receive.component.html',
  styleUrls: ['./receive.component.css'],
})

export class ReceiveComponent implements OnInit {
  constructor(private FullNodeApiService: FullNodeApiService, private globalService: GlobalService, private themeService: ThemeService, public ref: DynamicDialogRef) {
    this.isDarkTheme = themeService.getCurrentTheme().themeType == 'dark';
  }

  public address: any = "";
  public qrString: any;
  public copied: boolean = false;
  public showAll = false;
  public allAddresses: any;
  public usedAddresses: string[];
  public unusedAddresses: string[];
  public changeAddresses: string[];
  public pageNumberUsed: number = 1;
  public pageNumberUnused: number = 1;
  public pageNumberChange: number = 1;
  public isDarkTheme = false;
  public types: SelectItem[];
  public copyType: SelectItem[];
  public selectedType: string;

  private errorMessage: string;

  ngOnInit() {
    this.getUnusedReceiveAddresses();
    this.types = [
      { label: 'Unused Addresses', value: 'Unused', icon: 'fa fa-file-o' },
      { label: 'Used Addresses', value: 'Used', icon: 'fa fa-file-text-o' },
      { label: 'Change Addresses', value: 'Change', icon: 'fa fa-files-o' }
    ];
    this.selectedType = "Unused";

    this.copyType = [
      { label: 'Copy', value: 'Copy', icon: 'pi pi-copy' }
    ];
  }

  public onCopiedClick() {
    this.copied = true;
  }

  public showAllAddresses() {
    this.showAll = true;
    this.getAddresses();
  }

  public showOneAddress() {
    this.getUnusedReceiveAddresses();
    this.showAll = false;
  }

  private getUnusedReceiveAddresses() {
    let walletInfo = new WalletInfo(this.globalService.getWalletName())
    this.FullNodeApiService.getUnusedReceiveAddress(walletInfo)
      .subscribe(
        response => {
          this.address = response;
          this.qrString = response;
        }
      );
  }

  private getAddresses() {
    let walletInfo = new WalletInfo(this.globalService.getWalletName())
    this.FullNodeApiService.getAllAddresses(walletInfo)
      .subscribe(
        response => {
          this.allAddresses = [];
          this.usedAddresses = [];
          this.unusedAddresses = [];
          this.changeAddresses = [];
          this.allAddresses = response.addresses;

          for (let address of this.allAddresses) {
            if (address.isUsed) {
              this.usedAddresses.push(address.address);
            } else if (address.isChange) {
              this.changeAddresses.push(address.address);
            } else {
              this.unusedAddresses.push(address.address);
            }
          }
        }
      );
  }
}
