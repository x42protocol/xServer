import { Component, OnInit } from '@angular/core';
import { DynamicDialogRef} from 'primeng/api';
import { Router } from '@angular/router';

import { FullNodeApiService } from '../../shared/services/fullnode.api.service';
import { GlobalService } from '../../shared/services/global.service';

@Component({
  selector: 'app-logout-confirmation',
  templateUrl: './logout-confirmation.component.html',
  styleUrls: ['./logout-confirmation.component.css']
})
export class LogoutConfirmationComponent implements OnInit {

  constructor(private router: Router, private FullNodeApiService: FullNodeApiService, private globalService: GlobalService, public ref: DynamicDialogRef) { }

  public sidechainEnabled: boolean;

  ngOnInit() {
    this.sidechainEnabled = this.globalService.getSidechainEnabled();
  }

  public onLogout() {
    if (!this.sidechainEnabled) {
      this.FullNodeApiService.stopStaking()
        .subscribe();
    }
    this.ref.close();
    this.router.navigate(['/login']);
  }
}
