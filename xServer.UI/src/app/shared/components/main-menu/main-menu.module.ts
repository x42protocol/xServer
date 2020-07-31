import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MainMenuComponent } from './main-menu.component';
import { StatusBarComponent } from '../../../wallet/status-bar/status-bar.component';

// PrimeNG Components.
import { MenubarModule } from 'primeng/menubar';
import { MenuModule } from 'primeng/menu';
import { ButtonModule } from 'primeng/button';
import { SidebarModule } from 'primeng/sidebar';
import { DropdownModule } from 'primeng/dropdown';
import { TooltipModule } from 'primeng/tooltip';

@NgModule({
  imports: [
    CommonModule,
    ButtonModule,
    MenubarModule,
    MenuModule,
    SidebarModule,
    DropdownModule,
    FormsModule,
    TooltipModule
  ],
  exports: [MainMenuComponent],
  declarations: [
    MainMenuComponent,
    StatusBarComponent
  ],
})
export class MainMenuModule {
}
