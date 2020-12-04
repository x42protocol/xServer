import { Component, HostBinding, NgZone, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { ApplicationStateService } from '../app/shared/services/application-state.service';
import { ApiService } from '../app/shared/services/fullnode.api.service';
import { Logger } from '../app/shared/services/logger.service';
import { ElectronService } from 'ngx-electron';
import { ThemeService } from './shared/services/theme.service';
import { TitleService } from './shared/services/title.service';
import { NodeStatus } from './shared/models/node-status';
import { MenuItem } from 'primeng/api';
import { Observable, Subscription } from 'rxjs';
import { delay, retryWhen, tap, finalize } from 'rxjs/operators';
import { environment } from '../environments/environment';
import * as signalR from '@aspnet/signalr';
import * as coininfo from 'x42-coininfo';
import { ServerApiService } from './shared/services/server.api.service';
import { ServerStatus } from './shared/models/server-status';

export interface ListItem {
  name: string;
  id: string;
}

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
})
export class AppComponent implements OnInit, OnDestroy {
  @HostBinding('class.load') hostClass = true;

  selectedMode: ListItem;
  selectedNetwork: ListItem;
  hasWallet = false;
  modes: ListItem[] = [];
  networks: ListItem[] = [];
  remember: boolean;
  connection: signalR.HubConnection;
  delayed = false;
  loading = true;
  loadingFailed = false;

  nodeStarted = false;
  nodeFailed = false;
  serverStarted = false;
  serverFailed = false;
  mainWalletExists = false;

  private subscription: Subscription;
  private readonly TryDelayMilliseconds = 3000;
  private readonly MaxRetryCount = 50;
  private databaseConnected: boolean;

  public apiConnected = false;
  public contextMenuItems: MenuItem[];
  public nodeStatusSubscription: Subscription;

  constructor(
    private themeService: ThemeService,
    private electronService: ElectronService,
    private router: Router,
    private log: Logger,
    private zone: NgZone,
    private apiService: ApiService,
    private serverApiService: ServerApiService,
    public appState: ApplicationStateService,
    private readonly titleService: TitleService,
  ) {

    this.modes = [
      { id: 'full', name: 'Full' },
    ];

    if (!environment.production) {
      this.modes.push({ id: 'demo', name: 'Demo' }, // Auto-wallet creation, etc.
        { id: 'local', name: 'Local' }, // Launches the daemon by specifying path to .dll file.
        { id: 'manual', name: 'Manual' }, // Manual startup of daemon, does not send shutdown messages.
        { id: 'simple', name: 'Mobile' }, // API Wallet mode.
        { id: 'light', name: 'Light' }, // Full Node in Purge mode and other features disabled.
        { id: 'pos', name: 'Point-of-Sale (POS)' },
        { id: 'readonly', name: 'Read-only' });
    }

    this.networks = appState.networks;

    this.selectedMode = this.modes.find(mode => mode.id === this.appState.mode);
    this.selectedNetwork = this.networks.find(network => network.id === this.appState.network);
    this.remember = true;

    this.log.info('Mode:', this.selectedMode);
    this.log.info('Network:', this.selectedNetwork);
    this.log.info('Node App State:', JSON.stringify(this.appState.daemon));
  }

  ngOnInit() {
    this.themeService.setTheme();
    this.initialize();

    this.contextMenuItems = [
      {
        label: 'x42 xCore ' + this.appState.version,
        icon: 'pi goat-icon'
      }
    ];
  }

  private cancelSubscriptions() {
    if (this.nodeStatusSubscription) {
      this.nodeStatusSubscription.unsubscribe();
    }
    if (this.subscription) {
      this.subscription.unsubscribe();
    }
  }

  get appTitle$(): Observable<string> {
    return this.titleService.$title;
  }

  initialize() {
    this.apiService.initialize();
    const network = coininfo('x42').toBitcoinJS();
    this.appState.networkDefinition = network;

    this.appState.networkParams = {
      private: network.wif,
      public: network.pubKeyHash
    };

    if (this.appState.mode === 'full' || this.appState.mode === 'local' || this.appState.mode === 'light') {
      this.loading = true;
      this.appState.connected = false;
      this.processesConnect();
    } else if (this.appState.mode === 'manual') {
      this.loading = false;
      this.appState.connected = true;
      this.processesConnect();
    } else if (this.appState.mode === 'simple') {
      // TODO: Should send the correct network, hard-coded to x42 main for now.
      // const network = coininfo('x42').toBitcoinJS();
      // this.appState.networkDefinition = network;

      // this.appState.networkParams = {
      //     private: network.wif,
      //     public: network.pubKeyHash
      // };

      this.appState.connected = true;
      this.startServer();
    }
  }

  onDaemonFolderChange(event) {
    this.log.info('Daemon folder changed:', event);

    if (event.target.files.length > 0) {
      this.appState.daemon.path = event.target.files[0].path;
    } else {
      this.appState.daemon.path = '';
    }
  }

  onDataFolderChange(event) {
    this.log.info('Data folder changed:', event);

    if (event.target.files.length > 0) {
      this.appState.daemon.datafolder = event.target.files[0].path;
    } else {
      this.appState.daemon.datafolder = '';
    }
  }

  launch() {
    this.appState.updateNetworkSelection(this.remember, this.selectedMode.id, this.selectedNetwork.id, this.appState.daemon.path, this.appState.daemon.datafolder);

    // If the selected mode is not 'local', we'll reset the path and data folder.
    if (this.appState.mode !== 'local') {
      localStorage.removeItem('Network:Path');
      localStorage.removeItem('Network:DataFolder');
    }

    this.initialize();
  }

  processesConnect() {
    // Do we need to keep a pointer to this timeout and remove it, or does the zone handle that?
    this.zone.run(() => {
      setTimeout(() => {
        this.delayed = true;
      }, 60000); // 60000 Make sure it is fairly high, we don't want users to immediatly perform advanced reset options when they don't need to.
    });

    this.tryStartNode();
    this.tryStartServer();
  }

  nodeFailedToLoad() {
    this.nodeFailed = true;
    this.loading = false;
    this.loadingFailed = true;
  }

  serverFailedToLoad() {
    this.serverFailed = true;
    this.loading = false;
    this.loadingFailed = true;
  }

  nodeLoaded() {
    this.nodeStarted = true;
    if (this.nodeStarted && this.serverStarted) {
      this.checkForMainWallet();
    }
  }

  serverLoaded() {
    this.serverStarted = true;
    if (this.nodeStarted && this.serverStarted) {
      this.checkForMainWallet();
    }
  }

  startServer() {
    setTimeout(() => {
      this.loading = false;
      // We have successful connection with daemon, make sure we inform the main process of |.
      this.electronService.ipcRenderer.send('daemon-started');
      this.appState.connected = true;
      if (!this.databaseConnected || !this.mainWalletExists) {
        this.router.navigate(['setup']);
      } else {
        this.router.navigate(['login']);
      }
    }, 2000);
  }

  private checkForMainWallet() {
    this.apiService.getWalletFiles()
      .subscribe(
        response => {
          if (response.walletsFiles.length > 0) {
            if (response.walletsFiles.includes('x42ServerMain.wallet.json')) {
              this.mainWalletExists = true;
            }
          } else {
            this.mainWalletExists = false;
          }
          this.startServer();
        }
      );
  }

  // Attempts to initialise the fullnode by contacting the daemon.  Will try to do this MaxRetryCount times.
  private tryStartNode() {
    let retry = 0;
    const stream$ = this.apiService.getNodeStatus(true).pipe(
      retryWhen(errors =>
        errors.pipe(delay(this.TryDelayMilliseconds)).pipe(
          tap(errorStatus => {
            if (retry++ === this.MaxRetryCount) {
              throw errorStatus;
            }
            console.log(`Retrying Node ${retry}...`);
          })
        )
      )
    );

    this.subscription = stream$.subscribe(
      (data: NodeStatus) => {
        this.nodeLoaded();
      }, (error: any) => {
        console.log('Failed to start node');
        this.nodeFailedToLoad();
      }
    );

  }

  // Attempts to initialise the xserver daemon.  Will try to do this MaxRetryCount times.
  private tryStartServer() {
    let retry = 0;
    const stream$ = this.serverApiService.getServerStatus(true).pipe(
      retryWhen(errors =>
        errors.pipe(delay(this.TryDelayMilliseconds)).pipe(
          tap(errorStatus => {
            if (retry++ === this.MaxRetryCount) {
              throw errorStatus;
            }
            console.log(`Retrying xServer ${retry}...`);
          })
        )
      )
    );

    this.subscription = stream$.subscribe(
      (data: ServerStatus) => {
        this.databaseConnected = data.databaseConnected;
        this.serverLoaded();
      }, (error: any) => {
        console.log('Failed to start server');
        this.serverFailedToLoad();
      }
    );

  }

  ngOnDestroy() {
    this.cancelSubscriptions();
  }

  cancel() {
    this.appState.connected = false;
    this.loading = false;
    this.delayed = false;
    this.appState.mode = null;
  }

  simpleWalletConnect() {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:4337/node')
      .build();

    this.connection.on('BlockConnected', (block) => {
      console.log('BlockConnected:' + block);
    });

    this.connection.on('TransactionReceived', (trx) => {
      console.log('TransactionReceived:' + trx);
    });

    this.connection.on('txs', (transactions) => {
      console.log(transactions);
      // TODO: Update a bitcore-lib fork to add support for x42.
      // var tx1 = transactions[0];
      // var tx = bitcoin.Transaction.fromHex(tx1.value.hex);
    });

    const self = this;
    // Transport fallback functionality is now built into start.
    this.connection.start()
      .then(() => {
        console.log('connection started');
        self.connection.send('Subscribe', { events: ['TransactionReceived', 'BlockConnected'] });
      })
      .catch(error => {
        console.error(error.message);
      });
  }

  public openSupport() {
    this.electronService.shell.openExternal('https://github.com/x42protocol/xServer/blob/master/README.md');
  }
}
