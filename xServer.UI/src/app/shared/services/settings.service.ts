import { Injectable } from '@angular/core';
import { ElectronService } from 'ngx-electron';
import { StorageService } from './storage.service';

@Injectable({
    providedIn: 'root'
})
export class SettingsService {
    constructor(
        private electron: ElectronService,
        private storage: StorageService) {

    }

    /** The UI mode of the application. Defaults to basic for most users. */
    get mode(): string {
        return this.storage.getValue('Settings:Mode', 'basic');
    }

    set mode(value: string) {
        this.storage.setValue('Settings:Mode', value);
    }

    /** Set different wallet modes, the default is multi. Single address mode is not adviceable and can have unexpected effects. */
    get walletMode(): string {
        return this.storage.getValue('Settings:WalletMode', 'multi');
    }

    set walletMode(value: string) {
        this.storage.setValue('Settings:WalletMode', value);
    }

    get hubs(): any {
        const hubs = this.storage.getJSON('Settings:Hubs', '[]');

        if (hubs === 'undefined') {
            return [];
        }

        return hubs;
    }

    set hubs(value: any) {
        this.storage.setJSON('Settings:Hubs', value);
    }

    get hub(): string {
        return this.storage.getValue('Settings:Hub');
    }

    set hub(value: string) {
        this.storage.setValue('Settings:Hub', value);
    }

    // get identities(): any {
    //     return this.storage.getJSON('Settings:Identities');
    // }

    // set identities(value: any) {
    //     this.storage.setJSON('Settings:Identities', value);
    // }

    // get identity(): string {
    //     return this.storage.getValue('Settings:Identity');
    // }

    // set identity(value: string) {
    //     this.storage.setValue('Settings:Identity', value);
    // }

    get language(): string {
        return this.storage.getValue('Settings:Language');
    }

    set language(value: string) {
        this.storage.setValue('Settings:Language', value);
    }

    get currency(): string {
        return this.storage.getValue('Settings:Currency');
    }

    set currency(value: string) {
        this.storage.setValue('Settings:Currency', value);
    }

    get showInTaskbar(): boolean {
        if (this.storage.getValue('Settings:ShowInTaskbar') === null) {
            return true;
        }

        return this.storage.getValue('Settings:ShowInTaskbar') === 'true';
    }

    set showInTaskbar(value: boolean) {
        this.storage.setValue('Settings:ShowInTaskbar', value.toString());
    }

    get openOnLogin(): boolean {
        return this.storage.getValue('Settings:OpenOnLogin') === 'true';
    }

    set openOnLogin(value: boolean) {
        this.storage.setValue('Settings:OpenOnLogin', value.toString());
    }

    /** NOT IMPLEMENTED. Used to automatically lock the wallet after a certain time. */
    get autoLock(): boolean {
        return this.storage.getValue('Settings:AutoLock') === 'true';
    }

    set autoLock(value: boolean) {
        this.storage.setValue('Settings:AutoLock', value.toString());
    }

    /** NOT IMPLEMENTED. Used to automatically clear the wallet, not persist it on exit. */
    get clearOnExit(): boolean {
        return this.storage.getValue('Settings:ClearOnExit') === 'true';
    }

    set clearOnExit(value: boolean) {
        this.storage.setValue('Settings:ClearOnExit', value.toString());
    }
}
