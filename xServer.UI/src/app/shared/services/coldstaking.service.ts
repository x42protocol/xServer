import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient, HttpParams, HttpErrorResponse } from '@angular/common/http';
import { Observable, interval, throwError } from 'rxjs';
import { catchError, startWith, switchMap } from 'rxjs/operators';
import { ApplicationStateService } from './application-state.service';
import { ChainService } from './chain.service';
import { ModalService } from './modal.service';
import { ColdStakingSetup } from '../models/coldstakingsetup';
import { ColdStakingSetupResponse } from '../models/coldstakingsetupresponse';
import { ColdStakingCreateAddressResponse } from '../models/coldstakingcreateaddressresponse';
import { ColdStakingCreateAccountResponse } from '../models/coldstakingcreateaccountresponse';
import { ColdStakingCreateAccountRequest } from '../models/coldstakingcreateaccountrequest';
import { ColdStakingGetInfoResponse } from '../models/coldstakinggetinforesponse';
import { ColdStakingWithdrawalResponse } from '../models/coldstakingwithdrawalresponse';
import { ColdStakingWithdrawalRequest } from '../models/coldstakingwithdrawalrequest';
import { AppConfigService } from './appconfig.service';


@Injectable({
  providedIn: 'root'
})
export class ColdStakingService {
  constructor(
    private router: Router,
    private http: HttpClient,
    private chains: ChainService,
    private modalService: ModalService,
    private appState: ApplicationStateService,
    private appConfigService: AppConfigService
  ) {
    this.initialize();
  }

  private pollingInterval = interval(5000);
  private x42ApiUrl;

  initialize() {
    const chain = this.chains.getChain(this.appState.network);
    this.setApiUrl(chain.apiPort);
  }

  setApiUrl(port: number) {
    this.x42ApiUrl = this.appConfigService.getConfig().fullNodeEndpoint;
  }

  getInfo(walletName: string): Observable<ColdStakingGetInfoResponse> {
    const params = new HttpParams()
      .set('walletName', walletName);

    return this.pollingInterval.pipe(
      startWith(0),
      switchMap(() => this.http.get<ColdStakingGetInfoResponse>(this.x42ApiUrl + '/coldstaking/cold-staking-info', { params })),
      catchError(err => this.handleHttpError(err))
    );
  }

  getAddress(walletName: string, isColdWalletAddress: boolean, segwit: boolean): Observable<ColdStakingCreateAddressResponse> {
    const params = new HttpParams()
      .set('walletName', walletName)
      .set('Segwit', segwit.toString().toLowerCase())
      .set('isColdWalletAddress', isColdWalletAddress.toString().toLowerCase());

    return this.http.get<ColdStakingCreateAddressResponse>(this.x42ApiUrl + '/coldstaking/cold-staking-address', { params }).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  createColdstaking(coldStakingSetup: ColdStakingSetup): Observable<ColdStakingSetupResponse> {
    return this.http.post<ColdStakingSetupResponse>(this.x42ApiUrl + '/coldstaking/setup-cold-staking', JSON.stringify(coldStakingSetup)).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  createColdStakingAccount(walletName: string, walletPassword: string, isColdWalletAddress: boolean): Observable<ColdStakingCreateAccountResponse> {
    const request = new ColdStakingCreateAccountRequest(walletName, walletPassword, isColdWalletAddress);
    return this.http.post<ColdStakingCreateAccountResponse>(this.x42ApiUrl + '/coldstaking/cold-staking-account', JSON.stringify(request)).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  withdrawColdStaking(coldStakingWithdrawalRequest: ColdStakingWithdrawalRequest): Observable<ColdStakingWithdrawalResponse> {
    return this.http.post<ColdStakingWithdrawalResponse>(this.x42ApiUrl + '/coldstaking/cold-staking-withdrawal', JSON.stringify(coldStakingWithdrawalRequest)).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  private handleHttpError(error: HttpErrorResponse, silent?: boolean) {
    console.log(error);
    if (error.status === 0) {
      if (!silent) {
        this.modalService.openModal(null, null);
        this.router.navigate(['app']);
      }
    } else if (error.status >= 400) {
      if (!error.error.errors[0].message) {
        console.log(error);
      }
      else {
        this.modalService.openModal(null, error.error.errors[0].message);
      }
    }
    return throwError(error);
  }
}
