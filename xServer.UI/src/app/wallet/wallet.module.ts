import { NgModule } from '@angular/core';
import { SharedModule } from '../shared/shared.module';
import { WalletRoutingModule } from './wallet-routing.module';
import { MainMenuModule } from '../shared/components/main-menu/main-menu.module';

import { WalletComponent } from './wallet.component';
import { MenuComponent } from './menu/menu.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { HistoryComponent } from './history/history.component';
import { AdvancedComponent } from './advanced/advanced.component';
import { ExtPubkeyComponent } from './advanced/components/ext-pubkey/ext-pubkey.component';
import { AboutComponent } from './advanced/components/about/about.component';
import { GenerateAddressesComponent } from './advanced/components/generate-addresses/generate-addresses.component';
import { ResyncComponent } from './advanced/components/resync/resync.component';
import { TransactionDetailsComponent } from './transaction-details/transaction-details.component';
import { LogoutConfirmationComponent } from './logout-confirmation/logout-confirmation.component';
import { CreateServerIDComponent } from './server/create-serverid/create-serverid.component';

// PrimeNG Components.
import { MessageService } from 'primeng/api';
import { DialogService } from 'primeng/dynamicdialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MenubarModule } from 'primeng/menubar';
import { SidebarModule } from 'primeng/sidebar';
import { DropdownModule } from 'primeng/dropdown';
import { FieldsetModule } from 'primeng/fieldset';
import { DialogModule } from 'primeng/dialog';
import { MessagesModule } from 'primeng/messages';
import { MessageModule } from 'primeng/message';
import { BlockUIModule } from 'primeng/blockui';
import { PanelModule } from 'primeng/panel';
import { DynamicDialogModule } from 'primeng/dynamicdialog';
import { TableModule } from 'primeng/table';
import { SelectButtonModule } from 'primeng/selectbutton';
import { MenuModule } from 'primeng/menu';
import { CalendarModule } from 'primeng/calendar';
import { ProgressBarModule } from 'primeng/progressbar';

@NgModule({
  imports: [
    SharedModule,
    WalletRoutingModule,
    MainMenuModule,
    ButtonModule,
    InputTextModule,
    MenubarModule,
    SidebarModule,
    DropdownModule,
    FieldsetModule,
    DialogModule,
    MessagesModule,
    MessageModule,
    BlockUIModule,
    PanelModule,
    DynamicDialogModule,
    TableModule,
    SelectButtonModule,
    MenuModule,
    CalendarModule,
    ProgressBarModule
  ],
  declarations: [
    WalletComponent,
    MenuComponent,
    DashboardComponent,
    TransactionDetailsComponent,
    LogoutConfirmationComponent,
    HistoryComponent,
    AdvancedComponent,
    ExtPubkeyComponent,
    AboutComponent,
    GenerateAddressesComponent,
    ResyncComponent,
    CreateServerIDComponent
  ],
  providers: [
    MessageService,
    DialogService
  ],
  entryComponents: [
    TransactionDetailsComponent,
    LogoutConfirmationComponent,
    CreateServerIDComponent
  ]
})

export class WalletModule { }
