import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CoinNotationPipe } from './pipes/coin-notation.pipe';
import { AutoFocusDirective } from './directives/auto-focus.directive';
import { PasswordValidationDirective } from './directives/password-validation.directive';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { NgxQRCodeModule } from 'ngx-qrcode2';
import { NgxPaginationModule } from 'ngx-pagination';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { GenericModalComponent } from './components/generic-modal/generic-modal.component';
import { SizeUnitPipe } from './pipes/size-unit.pipe';

// PrimeNG Components.
import { ButtonModule } from 'primeng/button';
import { DynamicDialogModule, DialogService } from 'primeng/dynamicdialog';

@NgModule({
  imports: [CommonModule, ButtonModule, DynamicDialogModule],
  declarations: [CoinNotationPipe, AutoFocusDirective, PasswordValidationDirective, GenericModalComponent, SizeUnitPipe],
  exports: [CommonModule, ReactiveFormsModule, FormsModule, NgbModule, NgxQRCodeModule, NgxPaginationModule, GenericModalComponent, CoinNotationPipe, AutoFocusDirective, PasswordValidationDirective, SizeUnitPipe],
  entryComponents: [GenericModalComponent],
  providers: [DialogService]
})

export class SharedModule { }
