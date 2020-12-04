import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpErrorResponse } from '@angular/common/http';
import { Observable, interval, throwError } from 'rxjs';
import { catchError, switchMap, startWith } from 'rxjs/operators';
import { ChainService } from './chain.service';
import { ApplicationStateService } from './application-state.service';
import { ModalService } from './modal.service';
import { ServerStatus } from '../models/server-status';
import { ServerSetupStatusResponse } from '../models/server-setupstatusresponse';
import { ServerSetupRequest } from '../models/server-setuprequest';
import { ProfileResult } from '../models/profileresult';
import { ServerSetupResponse } from '../models/server-setupresponse';
import { ServerStartRequest } from '../models/server-start-request';
import { Logger } from './logger.service';
import { ElectronService } from 'ngx-electron';
import { NotificationService } from './notification.service';


@Injectable({
  providedIn: 'root'
})
export class ServerApiService {
  constructor(
    private log: Logger,
    private http: HttpClient,
    private chains: ChainService,
    private modalService: ModalService,
    private electronService: ElectronService,
    private appState: ApplicationStateService,
    private notifications: NotificationService,
  ) {
    this.initialize();
  }

  private daemon;
  private pollingInterval = interval(3000);
  private x42ApiUrl;

  /** Initialized the daemon running in the background, by sending configuration that has been picked by user, including chain, network and mode. */
  initialize() {
    // Get the current network (main, regtest, testnet), current blockchain (x42, bitcoin) and the mode (full, light, mobile)
    const chain = this.chains.getChain(this.appState.network);

    // Get the correct name of the chain that was found.
    this.appState.networkName = chain.networkname;

    // Make sure we copy some of the state information to the chain instance supplied to launch the daemon by the main process.
    chain.mode = this.appState.daemon.mode;
    chain.path = this.appState.daemon.path;
    chain.datafolder = this.appState.daemon.datafolder;

    this.log.info('xServer.D Api Service, Chain: ', chain);

    if (this.electronService.ipcRenderer) {
      this.daemon = this.electronService.ipcRenderer.sendSync('start-xserver-daemon', chain);

      if (this.daemon !== 'OK') {
        this.notifications.add({
          title: 'xServer.D background error',
          hint: 'Messages from the background process received in xServer',
          message: this.daemon,
          icon: (this.daemon.indexOf('xServer was started in development mode') > -1) ? 'build' : 'warning'
        });
      }

      this.log.info('xServer.D result: ', this.daemon);
      this.setApiUrl(chain.xServerPort);
    }
  }

  setApiUrl(port: number) {
    this.x42ApiUrl = 'http://localhost:' + port;
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
    );
  }

  getServerSetupStatusInterval(): Observable<ServerSetupStatusResponse> {
    return this.pollingInterval.pipe(
      startWith(0),
      switchMap(() => this.http.get<ServerSetupStatusResponse>(this.x42ApiUrl + '/getserversetupstatus')),
      catchError(err => this.handleHttpError(err))
    );
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
    const params = new HttpParams()
      .set('name', name);
    return this.http.get<ProfileResult>(this.x42ApiUrl + '/getprofile', { params }).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Get profile information by profile key address
   */
  getProfileByKeyAddress(keyAddress: string): Observable<ProfileResult> {
    const params = new HttpParams()
      .set('keyAddress', keyAddress);
    return this.http.get<ProfileResult>(this.x42ApiUrl + '/getprofile', { params }).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Start the xServer
   */
  startxServer(serverStartRequest: ServerStartRequest): Observable<any> {
    return this.http.post(this.x42ApiUrl + '/start', JSON.stringify(serverStartRequest)).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Stop the xServer
   */
  stopxServer(): Observable<any> {
    return this.http.post(this.x42ApiUrl + '/stop', 'true').pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Shutdown the xServer
   */
  shutDown(): Observable<any> {
    return this.http.post(this.x42ApiUrl + '/shutdown', 'true').pipe(
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
