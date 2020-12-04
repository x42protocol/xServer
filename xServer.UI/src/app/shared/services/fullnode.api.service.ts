import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpErrorResponse } from '@angular/common/http';
import { Observable, interval, throwError } from 'rxjs';
import { catchError, switchMap, startWith } from 'rxjs/operators';
import { ModalService } from './modal.service';
import { AddressLabel } from '../models/address-label';
import { WalletCreation } from '../models/wallet-creation';
import { WalletRecovery } from '../models/wallet-recovery';
import { WalletLoad } from '../models/wallet-load';
import { WalletInfo } from '../models/wallet-info';
import { SidechainFeeEstimation } from '../models/sidechain-fee-estimation';
import { FeeEstimation } from '../models/fee-estimation';
import { TransactionBuilding } from '../models/transaction-building';
import { TransactionSending } from '../models/transaction-sending';
import { NodeStatus } from '../models/node-status';
import { WalletRescan } from '../models/wallet-rescan';
import { SignMessageRequest } from '../models/wallet-signmessagerequest';
import { VerifyRequest } from '../models/wallet-verifyrequest';
import { SplitCoins } from '../models/split-coins';
import { ValidateAddressResponse } from '../models/validateaddressresponse';
import { XServerStatus } from '../models/xserver-status';
import { Logger } from './logger.service';
import { ChainService } from './chain.service';
import { ApplicationStateService } from './application-state.service';
import { ElectronService } from 'ngx-electron';
import { NotificationService } from './notification.service';

@Injectable({
  providedIn: 'root'
})

export class ApiService {
  static singletonInstance: ApiService;

  constructor(
    private http: HttpClient,
    private log: Logger,
    private chains: ChainService,
    private appState: ApplicationStateService,
    private electronService: ElectronService,
    private notifications: NotificationService,
    private modalService: ModalService,
  ) {
    if (!ApiService.singletonInstance) {
      ApiService.singletonInstance = this;
    }

    return ApiService.singletonInstance;
  }

  private daemon;
  private pollingInterval = interval(5000);

  public apiUrl: string;
  public genesisDate: Date;
  public apiPort: number;

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

    this.genesisDate = chain.genesisDate;

    this.log.info('Node Api Service, Chain: ', chain);

    if (this.electronService.ipcRenderer) {
      this.daemon = this.electronService.ipcRenderer.sendSync('start-daemon', chain);

      if (this.daemon !== 'OK') {
        this.notifications.add({
          title: 'xServer Node background error',
          hint: 'Messages from the background process received in xServer',
          message: this.daemon,
          icon: (this.daemon.indexOf('xServer was started in development mode') > -1) ? 'build' : 'warning'
        });
      }

      this.log.info('Node result: ', this.daemon);
      this.setApiPort(chain.apiPort);
    }
  }

  /**
   * Set the API port to connect with full node API. This will differ depending on coin and network.
   */
  setApiPort(port: number) {
    this.apiPort = port;
    this.apiUrl = 'http://localhost:' + port + '/api';
  }


  getNodeStatus(silent?: boolean): Observable<NodeStatus> {
    return this.http.get<NodeStatus>(this.apiUrl + '/node/status').pipe(
      catchError(err => this.handleHttpError(err, silent))
    );
  }

  getNodeStatusInterval(): Observable<NodeStatus> {
    return this.pollingInterval.pipe(
      startWith(0),
      switchMap(() => this.http.get<NodeStatus>(this.apiUrl + '/node/status')),
      catchError(err => this.handleHttpError(err))
    );
  }

  getxServerStatusInterval(): Observable<XServerStatus> {
    return this.pollingInterval.pipe(
      startWith(0),
      switchMap(() => this.http.get<XServerStatus>(this.apiUrl + '/xServer/getxserverstats')),
      catchError(err => this.handleHttpError(err))
    );
  }

  getAddressBookAddresses(): Observable<any> {
    return this.pollingInterval.pipe(
      startWith(0),
      switchMap(() => this.http.get(this.apiUrl + '/AddressBook')),
      catchError(err => this.handleHttpError(err))
    );
  }

  addAddressBookAddress(data: AddressLabel): Observable<any> {
    return this.http.post(this.apiUrl + '/AddressBook/address', JSON.stringify(data)).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  removeAddressBookAddress(label: string): Observable<any> {
    const params = new HttpParams().set('label', label);
    return this.http.delete(this.apiUrl + '/AddressBook/address', { params }).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Gets available wallets at the default path
   */
  getWalletFiles(): Observable<any> {
    return this.http.get(this.apiUrl + '/wallet/files').pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /** Gets the extended public key from a certain wallet */
  getExtPubkey(data: WalletInfo): Observable<any> {
    const params = new HttpParams()
      .set('walletName', data.walletName)
      .set('accountName', 'account 0');

    return this.http.get(this.apiUrl + '/wallet/extpubkey', { params }).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Get a new mnemonic
   */
  getNewMnemonic(): Observable<any> {
    const params = new HttpParams()
      .set('language', 'English')
      .set('wordCount', '12');

    return this.http.get(this.apiUrl + '/wallet/mnemonic', { params }).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Create a new x42 wallet.
   */
  createX42Wallet(data: WalletCreation): Observable<any> {
    return this.http.post(this.apiUrl + '/wallet/create/', JSON.stringify(data)).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Recover a x42 wallet.
   */
  recoverX42Wallet(data: WalletRecovery): Observable<any> {
    return this.http.post(this.apiUrl + '/wallet/recover/', JSON.stringify(data)).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Load a x42 wallet
   */
  loadX42Wallet(data: WalletLoad): Observable<any> {
    return this.http.post(this.apiUrl + '/wallet/load/', JSON.stringify(data)).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Get wallet status info from the API.
   */
  getWalletStatus(): Observable<any> {
    return this.http.get(this.apiUrl + '/wallet/status').pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Get general wallet info from the API once.
   */
  getGeneralInfoOnce(data: WalletInfo): Observable<any> {
    const params = new HttpParams().set('Name', data.walletName);
    return this.http.get(this.apiUrl + '/wallet/general-info', { params }).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Get general wallet info from the API.
   */
  getGeneralInfo(data: WalletInfo, silent?: boolean): Observable<any> {
    const params = new HttpParams().set('Name', data.walletName);
    return this.pollingInterval.pipe(
      startWith(0),
      switchMap(() => this.http.get(this.apiUrl + '/wallet/general-info', { params })),
      catchError(err => this.handleHttpError(err, silent))
    );
  }

  /**
   * Get wallet balance info from the API.
   */
  getWalletBalance(data: WalletInfo): Observable<any> {
    const params = new HttpParams()
      .set('walletName', data.walletName)
      .set('accountName', data.accountName);
    return this.pollingInterval.pipe(
      startWith(0),
      switchMap(() => this.http.get(this.apiUrl + '/wallet/balance', { params })),
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Get the maximum sendable amount for a given fee from the API
   */
  getMaximumBalance(data): Observable<any> {
    const params = new HttpParams()
      .set('walletName', data.walletName)
      .set('accountName', 'account 0')
      .set('feeType', data.feeType)
      .set('allowUnconfirmed', 'true');
    return this.http.get(this.apiUrl + '/wallet/maxbalance', { params }).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Get a wallets transaction history info from the API.
   */
  getWalletHistory(data: WalletInfo): Observable<any> {
    const params = new HttpParams()
      .set('walletName', data.walletName)
      .set('accountName', data.accountName);
    return this.pollingInterval.pipe(
      startWith(0),
      switchMap(() => this.http.get(this.apiUrl + '/wallet/history', { params })),
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Get an unused receive address for a certain wallet from the API.
   */
  getUnusedReceiveAddress(data: WalletInfo): Observable<any> {
    const params = new HttpParams()
      .set('walletName', data.walletName)
      .set('accountName', 'account 0');
    return this.http.get(this.apiUrl + '/wallet/unusedaddress', { params }).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Get multiple unused receive addresses for a certain wallet from the API.
   */
  getUnusedReceiveAddresses(data: WalletInfo, count: string): Observable<any> {
    const params = new HttpParams()
      .set('walletName', data.walletName)
      .set('accountName', 'account 0')
      .set('count', count);
    return this.http.get(this.apiUrl + '/wallet/unusedaddresses', { params }).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Get get all addresses for an account of a wallet from the API.
   */
  getAllAddresses(data: WalletInfo): Observable<any> {
    const params = new HttpParams()
      .set('walletName', data.walletName)
      .set('accountName', data.accountName);
    return this.http.get(this.apiUrl + '/wallet/addresses', { params }).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Estimate the fee of a transaction
   */
  estimateFee(data: FeeEstimation): Observable<any> {
    return this.http.post(this.apiUrl + '/wallet/estimate-txfee', {
      walletName: data.walletName,
      accountName: data.accountName,
      recipients: [
        {
          destinationAddress: data.recipients[0].destinationAddress,
          amount: data.recipients[0].amount
        }
      ],
      feeType: data.feeType,
      allowUnconfirmed: true
    }).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Estimate the fee of a sidechain transaction
   */
  estimateSidechainFee(data: SidechainFeeEstimation): Observable<any> {
    return this.http.post(this.apiUrl + '/wallet/estimate-txfee', {
      walletName: data.walletName,
      accountName: data.accountName,
      recipients: [
        {
          destinationAddress: data.recipients[0].destinationAddress,
          amount: data.recipients[0].amount
        }
      ],
      feeType: data.feeType,
      allowUnconfirmed: true
    }).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Build a transaction
   */
  buildTransaction(data: TransactionBuilding): Observable<any> {
    return this.http.post(this.apiUrl + '/wallet/build-transaction', JSON.stringify(data)).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Send transaction
   */
  sendTransaction(data: TransactionSending): Observable<any> {
    return this.http.post(this.apiUrl + '/wallet/send-transaction', JSON.stringify(data)).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /** Remove transaction */
  removeTransaction(walletName: string): Observable<any> {
    const params = new HttpParams()
      .set('walletName', walletName)
      .set('all', 'true')
      .set('resync', 'true');
    return this.http.delete(this.apiUrl + '/wallet/remove-transactions', { params }).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /** Rescan wallet from a certain date using remove-transactions */
  rescanWallet(data: WalletRescan): Observable<any> {
    const params = new HttpParams()
      .set('walletName', data.name)
      .set('fromDate', data.fromDate.toDateString())
      .set('reSync', 'true');
    return this.http.delete(this.apiUrl + '/wallet/remove-transactions/', { params }).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Sign the given message with the private key of the given address
   */
  signMessage(data: SignMessageRequest): Observable<any> {
    return this.http.post(this.apiUrl + '/wallet/signmessage', JSON.stringify(data)).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Verify the given signature with the given address
   */
  verifyMessage(data: VerifyRequest): Observable<any> {
    return this.http.post(this.apiUrl + '/wallet/verifymessage', JSON.stringify(data)).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Start staking
   */
  startStaking(data: any): Observable<any> {
    return this.http.post(this.apiUrl + '/staking/startstaking', JSON.stringify(data)).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Get staking info
   */
  getStakingInfo(): Observable<any> {
    return this.pollingInterval.pipe(
      startWith(0),
      switchMap(() => this.http.get(this.apiUrl + '/staking/getstakinginfo')),
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Stop staking
   */
  stopStaking(): Observable<any> {
    return this.http.post(this.apiUrl + '/staking/stopstaking', 'true').pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Send shutdown signal to the daemon
   */
  shutdownNode(): Observable<any> {
    return this.http.post(this.apiUrl + '/node/shutdown', 'corsProtection:true').pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  /**
   * Get address information if valid.
   */
  validateAddress(address: string, silent?: boolean): Observable<ValidateAddressResponse> {
    const params = new HttpParams()
      .set('address', address);
    return this.http.get<ValidateAddressResponse>(this.apiUrl + '/node/validateaddress', { params }).pipe(
      catchError(err => this.handleHttpError(err, silent))
    );
  }

  /*
   * Posts a coin split request
   */
  postCoinSplit(splitCoins: SplitCoins): Observable<any> {
    return this.http.post(this.apiUrl + '/wallet/splitcoins', splitCoins).pipe(
      catchError(err => this.handleHttpError(err))
    );
  }

  private handleHttpError(error: HttpErrorResponse, silent?: boolean) {
    console.log(error);
    if (error.status >= 400) {
      if (error.error.errors[0].message && !silent) {
        this.modalService.openModal(null, error.error.errors[0].message);
      }
    }
    console.log(error);
    return throwError(error);
  }
}
