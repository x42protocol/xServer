import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ShutdownComponent } from './shutdown.component';
import { ButtonModule } from 'primeng/button';

@NgModule({
  imports: [CommonModule, ButtonModule],
  exports: [ShutdownComponent],
  declarations: [ShutdownComponent],
})
export class ShutdownModule {
}
