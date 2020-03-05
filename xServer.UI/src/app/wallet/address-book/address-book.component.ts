import { Component, OnInit, OnDestroy } from '@angular/core';
import { ClipboardService } from 'ngx-clipboard';
import { SelectItem } from 'primeng/api';
import { DialogService } from 'primeng/dynamicdialog';

import { FullNodeApiService } from '../../shared/services/fullnode.api.service';
import { SendComponent } from '../send/send.component';
import { AddNewAddressComponent } from '../address-book/modals/add-new-address/add-new-address.component';
import { AddressLabel } from '../../shared/models/address-label';

import { Subscription } from 'rxjs';

@Component({
  selector: 'app-address-book',
  templateUrl: './address-book.component.html',
  styleUrls: ['./address-book.component.css']
})
export class AddressBookComponent implements OnInit, OnDestroy {
  constructor(private FullNodeApiService: FullNodeApiService, private clipboardService: ClipboardService, public dialogService: DialogService) { }

  public copyType: SelectItem[];
  private addressBookSubcription: Subscription;
  addresses: AddressLabel[];

  ngOnInit() {
    this.startSubscriptions();
    this.copyType = [
      { label: 'Copy', value: 'Copy', icon: 'pi pi-copy' }
    ];
  }

  ngOnDestroy() {
    this.cancelSubscriptions();
  }

  private startSubscriptions() {
    this.getAddressBookAddresses();
  }

  private cancelSubscriptions() {
    if (this.addressBookSubcription) {
      this.addressBookSubcription.unsubscribe();
    }
  }

  private getAddressBookAddresses() {
    this.addressBookSubcription = this.FullNodeApiService.getAddressBookAddresses()
      .subscribe(
        response => {
          this.addresses = null;
          if (response.addresses[0]) {
            this.addresses = [];
            let addressResponse = response.addresses;
            for (let address of addressResponse) {
              this.addresses.push(new AddressLabel(address.label, address.address));
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
  }

  copyToClipboardClicked(address: AddressLabel) {
    if (this.clipboardService.copyFromContent(address.address)) {
    }
  }

  sendClicked(address: AddressLabel) {
    let modalData = {
      "address": address.address
    };

    this.dialogService.open(SendComponent, {
      header: 'Send to',
      width: '700px',
      data: modalData
    });
  }

  removeClicked(address: AddressLabel) {
    this.FullNodeApiService.removeAddressBookAddress(address.label)
      .subscribe(
        response => {
          this.cancelSubscriptions();
          this.startSubscriptions();
        }
      );
  }

  addNewAddressClicked() {
    this.dialogService.open(AddNewAddressComponent, {
      header: 'New Address',
      width: '500px',
    });
  }
}
