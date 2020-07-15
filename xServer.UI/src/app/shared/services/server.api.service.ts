import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient, HttpParams, HttpErrorResponse } from '@angular/common/http';
import { Observable, interval, throwError } from 'rxjs';
import { catchError, switchMap, startWith } from 'rxjs/operators';

import { GlobalService } from './global.service';
import { ModalService } from './modal.service';

import { ServerStatus } from '../models/server-status';
import { ServerSetupStatusResponse } from '../models/server-setupstatusresponse';
import { ServerSetupRequest } from '../models/server-setuprequest';
import { ColdStakingCreateAddressResponse } from '../models/coldstakingcreateaddressresponse';
import { ProfileResult } from '../models/profileresult';

@Injectable({
  providedIn: 'root'
})
export class ServerApiService {
  constructor(private http: HttpClient, private globalService: GlobalService, private modalService: ModalService, private router: Router) {
    this.setApiUrl();
  };

  private pollingInterval = interval(3000);
  private apiPort;
  private x42ApiUrl;

  setApiUrl() {
    this.apiPort = this.globalService.getServerApiPort();
    this.x42ApiUrl = 'http://localhost:' + this.apiPort;
  }

  getServerStatus(silent?: boolean): Observable<ServerStatus> {
    return this.http.get<ServerStatus>(this.x42ApiUrl + '/status').pipe(
      catchError(err => this.handleHttpError(err, silent))
    );
  }

  getServerStatusInterval(): Observable<ServerStatus> {
    return this.pollingInterval.pipe(
      startWith(0),
      switchMap(() => this.http.get<ServerStatus>(this.x42ApiUrl + '/status')),
      catchError(err => this.handleHttpError(err))
    )
  }

  getServerSetupStatusInterval(): Observable<ServerSetupStatusResponse> {
    return this.pollingInterval.pipe(
      startWith(0),
      switchMap(() => this.http.get<ServerSetupStatusResponse>(this.x42ApiUrl + '/getserversetupstatus')),
      catchError(err => this.handleHttpError(err))
    )
  }

  setSetupAddress(data: ServerSetupRequest): Observable<ServerSetupResponse> {
    return this.http.post<ServerSetupResponse>(this.x42ApiUrl + '/set-server-address', JSON.stringify(data)).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  getSetupAddress(): Observable<ServerSetupResponse> {
    return this.http.get<ServerSetupResponse>(this.x42ApiUrl + '/setup').pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
  * Get profile information by profile name
  */
  getProfileByName(name: string): Observable<ProfileResult> {
    let params = new HttpParams()
      .set('name', name);
    return this.http.get<ProfileResult>(this.x42ApiUrl + '/getprofile', { params }).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
  * Get profile information by profile key address
  */
  getProfileByKeyAddress(keyAddress: string): Observable<ProfileResult> {
    let params = new HttpParams()
      .set('keyAddress', keyAddress);
    return this.http.get<ProfileResult>(this.x42ApiUrl + '/getprofile', { params }).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }


  private handleHttpError(error: HttpErrorResponse, silent?: boolean) {
    console.log(error);
    if (error.status >= 400) {
      if (error.error.errors[0] && !silent) {
        this.modalService.openModal(null, error.error.errors[0]);
      }
    }
    console.log(error);
    return throwError(error);
  }
}
