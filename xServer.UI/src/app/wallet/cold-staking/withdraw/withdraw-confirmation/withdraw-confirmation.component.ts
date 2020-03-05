import { Component } from '@angular/core';
import { DynamicDialogRef } from 'primeng/dynamicdialog';

@Component({
  selector: 'app-withdraw-confirmation',
  templateUrl: './withdraw-confirmation.component.html',
  styleUrls: ['./withdraw-confirmation.component.css']
})
export class ColdStakingWithdrawConfirmationComponent {
  constructor(public ref: DynamicDialogRef) { }

  okClicked() {
    this.ref.close();
  }
}
