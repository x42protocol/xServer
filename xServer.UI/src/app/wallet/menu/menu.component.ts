import { Component } from '@angular/core';
import { Router } from '@angular/router';

import { GlobalService } from '../../shared/services/global.service';
import { LogoutConfirmationComponent } from '../logout-confirmation/logout-confirmation.component';
import { DialogService } from 'primeng/dynamicdialog';

@Component({
  selector: 'app-menu',
  templateUrl: './menu.component.html',
  styleUrls: ['./menu.component.css'],
})
export class MenuComponent {
  constructor(
    private globalService: GlobalService,
    private router: Router,
    public dialogService: DialogService,
  ) {
    this.walletName = this.globalService.getWalletName();
  }

  public walletName: string;


  openAddressBook() {
    this.router.navigate(['/wallet/address-book']);
  }

  openAdvanced() {
    this.router.navigate(['/wallet/advanced']);
  }

  logoutClicked() {
    this.dialogService.open(LogoutConfirmationComponent, {
      header: 'Lock'
    });
  }
}
