import { NgModule } from '@angular/core';
import { SharedModule } from '../shared/shared.module';
import { WalletRoutingModule } from './wallet-routing.module';
import { SmartContractsModule } from './smart-contracts/smart-contracts.module';
import { MainMenuModule } from '../shared/components/main-menu/main-menu.module';

import { WalletComponent } from './wallet.component';
import { MenuComponent } from './menu/menu.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { HistoryComponent } from './history/history.component';
import { AdvancedComponent } from './advanced/advanced.component';
import { AddressBookComponent } from './address-book/address-book.component';
import { AddNewAddressComponent } from './address-book/modals/add-new-address/add-new-address.component';
import { ExtPubkeyComponent } from './advanced/components/ext-pubkey/ext-pubkey.component';
import { AboutComponent } from './advanced/components/about/about.component';
import { GenerateAddressesComponent } from './advanced/components/generate-addresses/generate-addresses.component';
import { ResyncComponent } from './advanced/components/resync/resync.component';
import { SendComponent } from './send/send.component';
import { ReceiveComponent } from './receive/receive.component';
import { ColdStakingOverviewComponent } from './cold-staking/overview.component';
import { ColdStakingCreateComponent } from './cold-staking/create/create.component';
import { ColdStakingCreateSuccessComponent } from "./cold-staking/create-success/create-success.component";
import { ColdStakingCreateAddressComponent } from "./cold-staking/create-address/create-address.component";
import { ColdStakingWithdrawComponent } from "./cold-staking/withdraw/withdraw.component";
import { ColdStakingWithdrawConfirmationComponent } from "./cold-staking/withdraw/withdraw-confirmation/withdraw-confirmation.component";
import { ColdStakingCreateHotComponent } from './cold-staking/create-hot/create-hot.component';
import { SendConfirmationComponent } from './send/send-confirmation/send-confirmation.component';
import { TransactionDetailsComponent } from './transaction-details/transaction-details.component';
import { LogoutConfirmationComponent } from './logout-confirmation/logout-confirmation.component';
import { CreateServerIDComponent } from './server/create-serverid/create-serverid.component';
import { CollateralSetupComponent } from './collateral-setup/collateral-setup.component';

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

@NgModule({
  imports: [
    SharedModule,
    WalletRoutingModule,
    SmartContractsModule,
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
    CalendarModule
  ],
  declarations: [
    WalletComponent,
    MenuComponent,
    DashboardComponent,
    SendComponent,
    ReceiveComponent,
    SendConfirmationComponent,
    TransactionDetailsComponent,
    LogoutConfirmationComponent,
    HistoryComponent,
    AdvancedComponent,
    AddressBookComponent,
    AddNewAddressComponent,
    ExtPubkeyComponent,
    AboutComponent,
    GenerateAddressesComponent,
    ResyncComponent,
    ColdStakingOverviewComponent,
    ColdStakingCreateComponent,
    ColdStakingCreateSuccessComponent,
    ColdStakingCreateAddressComponent,
    ColdStakingWithdrawComponent,
    ColdStakingWithdrawConfirmationComponent,
    ColdStakingCreateHotComponent,
    CreateServerIDComponent,
    CollateralSetupComponent
  ],
  providers: [
    MessageService,
    DialogService
  ],
  entryComponents: [
    SendComponent,
    SendConfirmationComponent,
    ReceiveComponent,
    TransactionDetailsComponent,
    LogoutConfirmationComponent,
    ColdStakingCreateComponent,
    ColdStakingCreateSuccessComponent,
    ColdStakingCreateAddressComponent,
    ColdStakingWithdrawComponent,
    ColdStakingWithdrawConfirmationComponent,
    ColdStakingCreateHotComponent,
    CreateServerIDComponent
  ]
})

export class WalletModule { }
