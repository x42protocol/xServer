import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { MenuItem } from 'primeng/api';

@Component({
  selector: 'app-advanced',
  templateUrl: './advanced.component.html',
  styleUrls: ['./advanced.component.css']
})

export class AdvancedComponent implements OnInit, OnDestroy {

  constructor(private router: Router) {
  }
  public items: MenuItem[];

  ngOnInit() {
    this.items = [{
      label: '',
      items: [
        {
          label: 'About',
          icon: 'pi pi-info',
          command: (event: Event) => {
            this.router.navigate(['/wallet/advanced/about']);
          }
        },
        {
          label: 'Extended Public Key',
          icon: 'pi pi-key',
          command: (event: Event) => {
            this.router.navigate(['/wallet/advanced/extpubkey']);
          }
        },
        {
          label: 'Generate Addresses',
          icon: 'pi pi-list',
          command: (event: Event) => {
            this.router.navigate(['/wallet/advanced/generate-addresses']);
          }
        },
        {
          label: 'Rescan Wallet',
          icon: 'pi pi-refresh',
          command: (event: Event) => {
            this.router.navigate(['/wallet/advanced/resync']);
          }
        }
      ]
    }];
  }

  ngOnDestroy() {

  }
}
