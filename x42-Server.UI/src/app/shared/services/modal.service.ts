import { Injectable } from "@angular/core";
import { DialogService } from 'primeng/api';
import { GenericModalComponent } from '../components/generic-modal/generic-modal.component';

@Injectable({
  providedIn: 'root'
})
export class ModalService {
  constructor(public dialogService: DialogService) {}

  public openModal(title, message) {

    let showHeader: boolean = true;

    if (title == null) {
      showHeader = false;
    }

    let modalData = {
      "message": message
    };
    const modalRef = this.dialogService.open(GenericModalComponent,
      {
        header: title,
        data: modalData,
        showHeader: showHeader
      }
    );
  }
}
