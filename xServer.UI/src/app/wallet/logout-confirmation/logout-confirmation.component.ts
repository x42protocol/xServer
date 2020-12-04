import { Component } from '@angular/core';
import { DynamicDialogRef } from 'primeng/dynamicdialog';
import { Router } from '@angular/router';

import { ApiService } from '../../shared/services/fullnode.api.service';
import { GlobalService } from '../../shared/services/global.service';

@Component({
  selector: 'app-logout-confirmation',
  templateUrl: './logout-confirmation.component.html',
  styleUrls: ['./logout-confirmation.component.css']
})
export class LogoutConfirmationComponent {
  constructor(
    private router: Router,
    private apiService: ApiService,
    private globalService: GlobalService,
    public ref: DynamicDialogRef,
  ) { }

  public onLogout() {
    this.apiService.stopStaking().subscribe();
    this.globalService.setProfile(null);
    this.ref.close();
    this.router.navigate(['/login']);
  }
}
