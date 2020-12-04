import { Injectable } from '@angular/core';
import { ElectronService } from 'ngx-electron';
import { XServerStatus } from '../models/xserver-status';

@Injectable({
  providedIn: 'root'
})
export class GlobalService {
  constructor(
    private electronService: ElectronService,
  ) {
    this.setWalletName('');
  }

  private walletPath: string;
  private currentWalletName: string;
  private coinType: number;
  private coinName: string;
  private coinUnit: string;
  private network: string;
  private decimalLimit = 8;
  private walletKeyAddress: string;
  private blockHeight: number;
  private xServerStatus: XServerStatus;
  private profile: any;

  quitApplication() {
    this.electronService.remote.app.quit();
  }
  getWalletPath() {
    return this.walletPath;
  }

  setWalletPath(walletPath: string) {
    this.walletPath = walletPath;
  }

  getNetwork() {
    return this.network;
  }

  setNetwork(network: string) {
    this.network = network;
  }

  getWalletName() {
    return this.currentWalletName;
  }

  setWalletName(currentWalletName: string) {
    this.currentWalletName = currentWalletName;
  }

  getWalletKeyAddress() {
    return this.walletKeyAddress;
  }

  setWalletKeyAddress(keyaddress: string) {
    this.walletKeyAddress = keyaddress;
  }

  getCoinUnit() {
    return this.coinUnit;
  }

  setCoinUnit(coinUnit: string) {
    this.coinUnit = coinUnit;
  }

  getCoinName() {
    return this.coinName;
  }

  setCoinName(coinName: string) {
    this.coinName = coinName;
  }

  setBlockHeight(height: number) {
    this.blockHeight = height;
  }

  getBlockHeight() {
    return this.blockHeight;
  }

  setxServerStatus(xServerStatus: XServerStatus) {
    this.xServerStatus = xServerStatus;
  }

  getxServerStatus() {
    return this.xServerStatus;
  }

  setProfile(profileInfo: any) {
    this.profile = profileInfo;
  }

  getProfile() {
    return this.profile;
  }

  transform(value: number) {
    let temp;
    if (typeof value === 'number') {
      switch (this.getCoinUnit()) {
        case 'x42':
          temp = value / 100000000;
          return temp.toFixed(this.decimalLimit);
        case 'mx42':
          temp = value / 100000;
          return temp.toFixed(this.decimalLimit);
        case 'ux42':
          temp = value / 100;
          return temp.toFixed(this.decimalLimit);
        case 'Tx42':
          temp = value / 100000000;
          return temp.toFixed(this.decimalLimit);
        case 'Tmx42':
          temp = value / 100000;
          return temp.toFixed(this.decimalLimit);
        case 'Tux42':
          temp = value / 100;
          return temp.toFixed(this.decimalLimit);
      }
    }
  }
}
