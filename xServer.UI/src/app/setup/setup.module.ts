import { NgModule } from '@angular/core';
import { SetupComponent } from './setup.component';
import { CreateComponent } from './create/create.component';
import { FinalizeSetupComponent } from './finalize-setup/finalize-setup.component';
import { SharedModule } from '../shared/shared.module';
import { SetupRoutingModule } from './setup-routing.module';
import { RecoverComponent } from './recover/recover.component';
import { ShowMnemonicComponent } from './create/show-mnemonic/show-mnemonic.component';
import { ConfirmMnemonicComponent } from './create/confirm-mnemonic/confirm-mnemonic.component';
import { WizardModule } from '../shared/components/wizard/wizard.module'
import { ThemeService } from '../shared/services/theme.service';
import { MainMenuModule } from '../shared/components/main-menu/main-menu.module';

// PrimeNG Components.
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { StepsModule } from 'primeng/steps';
import { InputTextModule } from 'primeng/inputtext';
import { FieldsetModule } from 'primeng/fieldset';
import { DialogModule } from 'primeng/dialog';
import { MessagesModule } from 'primeng/messages';
import { MessageModule } from 'primeng/message';
import { BlockUIModule } from 'primeng/blockui';
import { PanelModule } from 'primeng/panel';

@NgModule({
  imports: [
    SetupRoutingModule,
    SharedModule,
    StepsModule,
    ButtonModule,
    WizardModule,
    InputTextModule,
    FieldsetModule,
    DialogModule,
    MessagesModule,
    MessageModule,
    BlockUIModule,
    PanelModule,
    MainMenuModule
  ],
  declarations: [
    CreateComponent,
    FinalizeSetupComponent,
    SetupComponent,
    RecoverComponent,
    ShowMnemonicComponent,
    ConfirmMnemonicComponent
  ],
  providers: [
    ThemeService,
    MessageService],
  bootstrap: [SetupComponent]
})

export class SetupModule { }
