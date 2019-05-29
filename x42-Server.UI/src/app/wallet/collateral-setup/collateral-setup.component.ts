import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';

import { FullNodeApiService } from '../../shared/services/fullnode.api.service';
import { ModalService } from '../../shared/services/modal.service';

@Component({
  selector: 'collateral-setup',
  templateUrl: './collateral-setup.component.html',
  styleUrls: ['./collateral-setup.component.css']
})
export class CollateralSetupComponent implements OnInit {
  constructor(private FullNodeApiService: FullNodeApiService) { }

  @Input() queryParams: any;
  @Output() success: EventEmitter<boolean> = new EventEmitter<boolean>();


  ngOnInit() {

  }

  onFinalize() {

  }
}
