import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MainMenuComponent } from './main-menu.component';

// PrimeNG Components.
import { MenubarModule } from 'primeng/menubar';
import { MenuModule } from 'primeng/menu';
import { ButtonModule } from 'primeng/button';
import { SidebarModule } from 'primeng/sidebar';
import { DropdownModule } from 'primeng/dropdown';

@NgModule({
  imports: [
    CommonModule,
    ButtonModule,
    MenubarModule,
    MenuModule,
    SidebarModule,
    DropdownModule,
    FormsModule
  ],
  exports: [MainMenuComponent],
  declarations: [MainMenuComponent],
})
export class MainMenuModule {
}
