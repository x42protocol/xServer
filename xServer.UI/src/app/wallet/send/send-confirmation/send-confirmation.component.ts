import { Component, OnInit } from '@angular/core';

import { GlobalService } from '../../../shared/services/global.service';

import { CoinNotationPipe } from '../../../shared/pipes/coin-notation.pipe';

import { DynamicDialogRef, DynamicDialogConfig } from 'primeng/dynamicdialog';

@Component({
  selector: 'app-send-confirmation',
  templateUrl: './send-confirmation.component.html',
  styleUrls: ['./send-confirmation.component.css']
})
export class SendConfirmationComponent implements OnInit {

  public transaction: any;
  public transactionFee: any;
  public sidechainEnabled: boolean;
  public opReturnAmount: number;
  public hasOpReturn: boolean;
  constructor(private globalService: GlobalService, public ref: DynamicDialogRef, public config: DynamicDialogConfig) { }

  public showDetails: boolean = false;
  public coinUnit: string;

  ngOnInit() {
    this.transaction = this.config.data.transaction;
    this.transactionFee = this.config.data.transactionFee;
    this.sidechainEnabled = this.config.data.sidechainEnabled;
    this.opReturnAmount = this.config.data.opReturnAmount;
    this.hasOpReturn = this.config.data.hasOpReturn;

    this.coinUnit = this.globalService.getCoinUnit();
    this.transactionFee = new CoinNotationPipe().transform(this.transactionFee);
    if (this.hasOpReturn) {
      this.opReturnAmount = new CoinNotationPipe().transform(this.opReturnAmount);
      this.transaction.amount = +this.transaction.recipients[0].amount + +this.transactionFee + +this.opReturnAmount;
    } else {
      this.transaction.amount = +this.transaction.recipients[0].amount + +this.transactionFee;
    }
  }

  toggleDetails() {
    this.showDetails = !this.showDetails;
  }
}
