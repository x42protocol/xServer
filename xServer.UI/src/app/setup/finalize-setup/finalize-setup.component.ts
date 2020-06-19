import { Component, OnInit, Input} from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'finalize-setup',
  templateUrl: './finalize-setup.component.html',
  styleUrls: ['./finalize-setup.component.css']
})
export class FinalizeSetupComponent implements OnInit {
  constructor(private router: Router) { }

  @Input() databaseConnected: boolean;

  walletCreated: boolean;
  @Input() set walletComplete(value: boolean) {
    this.walletCreated = value;
  }

  ngOnInit() {

  }

  onClose() {
    this.router.navigate(['/login/app-login']);
  }
}
