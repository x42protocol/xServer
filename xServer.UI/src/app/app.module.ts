import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { SharedModule } from './shared/shared.module';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { ApiInterceptor } from './shared/http-interceptors/api-interceptor';
import { LoginComponent } from './login/login.component';
import { SetupModule } from './setup/setup.module';
import { WalletModule } from './wallet/wallet.module';
import { ThemeService } from './shared/services/theme.service';
import { MainMenuModule } from './shared/components/main-menu/main-menu.module';

// PrimeNG Components.
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { StepsModule } from 'primeng/steps';
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
import { DynamicDialogModule, DialogService } from 'primeng/dynamicdialog';
import { TableModule } from 'primeng/table';
import { CalendarModule } from 'primeng/calendar';

@NgModule({
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    HttpClientModule,
    SharedModule,
    SetupModule,
    WalletModule,
    AppRoutingModule,
    ButtonModule,
    StepsModule,
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
    MainMenuModule,
    TableModule,
    CalendarModule
  ],
  declarations: [
    AppComponent,
    LoginComponent
  ],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: ApiInterceptor, multi: true },
    ThemeService,
    MessageService,
    DialogService
  ],
  bootstrap: [AppComponent]
})

export class AppModule { }
