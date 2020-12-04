import { Injectable } from '@angular/core';
import { DialogService } from 'primeng/dynamicdialog';
import { GenericModalComponent } from '../components/generic-modal/generic-modal.component';

@Injectable({
  providedIn: 'root'
})
export class ModalService {
  constructor(public dialogService: DialogService) { }

  public openModal(title, message) {

    let showHeader = true;

    if (title == null) {
      showHeader = false;
    }

    const modalData = {
      message
    };
    this.dialogService.open(GenericModalComponent,
      {
        header: title,
        data: modalData,
        showHeader
      }
    );
  }
}
