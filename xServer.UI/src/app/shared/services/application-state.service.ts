import { Injectable } from '@angular/core';
import { TitleService } from './title.service';
import { Observable } from 'rxjs';
import { SettingsService } from './settings.service';

export interface DaemonConfiguration {
  mode: string;
  network: string;
  path: string;
  datafolder: string;
}

export class ListItem {
  name: string;
  id: string;
}

@Injectable({
  providedIn: 'root'
})
export class ApplicationStateService {

  // TODO: Figure out when multiple instance of singleton services is fixed for lazy-loaded routing/modules in Angular.
  // See details here: https://github.com/angular/angular/issues/12889#issuecomment-395720894
  static singletonInstance: ApplicationStateService;

  constructor(
    private settings: SettingsService,
    private readonly titleService: TitleService,
  ) {
    if (!ApplicationStateService.singletonInstance) {

      this.networks = [
        { id: 'x42main', name: 'Main Network' },
        { id: 'x42test', name: 'Test Network' }
      ];

      this.networkName = this.getParam('networkname') || 'Main Network';

      // TODO: These properties are deprecated, refactor!
      this.mode = localStorage.getItem('Network:Mode') || 'full';
      this.network = localStorage.getItem('Network:Network') || 'x42main';
      this.path = localStorage.getItem('Network:Path') || '';

      this.daemon = {
        mode: localStorage.getItem('Network:Mode') || 'full',
        network: localStorage.getItem('Network:Network') || 'x42main',
        path: localStorage.getItem('Network:Path') || '',
        datafolder: localStorage.getItem('Network:DataFolder') || ''
      };


      ApplicationStateService.singletonInstance = this;
    }
    return ApplicationStateService.singletonInstance;
  }

  delegated = false;

  networkDefinition: any;

  networkParams: any;

  version: string;

  release: string;

  daemon: DaemonConfiguration;

  mode: string;

  network: string;

  networkName: string;

  networks: ListItem[] = [];

  path: string;

  pageMode = false;

  handset = false;

  fullHeight = false;

  shutdownInProgress = false;

  shutdownDelayed = false;

  dataDirectory: string;

  /** Indicates if we are connected from xCore with the x42 node. */
  connected = false;

  changingMode = false;

  fullNodeVersion: string;

  serverDVersion: string;

  protocolVersion: number;

  get appTitle$(): Observable<string> {
    return this.titleService.$title;
  }

  get isSimpleMode(): boolean {
    return this.mode === 'simple';
  }

  getParam(n) {
    const half = location.search.split(n + '=')[1];
    return half !== undefined ? decodeURIComponent(half.split('&')[0]) : null;
  }

  setVersion(version: string) {
    this.version = version;

    if (this.version) {
      const v = version.split('.');
      if (v.length === 3) {
        this.release = v[2];
      } else {
        this.release = version;
      }
    }
  }

  resetNetworkSelection() {
    localStorage.removeItem('Network:Mode');
    localStorage.removeItem('Network:Network');
    localStorage.removeItem('Network:Path');
    localStorage.removeItem('Network:DataFolder');
  }

  updateNetworkSelection(persist: boolean, mode: string, network: string, path: string, datafolder: string) {
    this.daemon.mode = mode;
    this.daemon.network = network;
    this.daemon.path = path;
    this.daemon.datafolder = datafolder;

    // TODO: Remove and depricate these properties.
    this.mode = mode;
    this.network = network;

    if (persist) {
      localStorage.setItem('Network:Mode', mode);
      localStorage.setItem('Network:Network', network);
      localStorage.setItem('Network:Path', path);
      localStorage.setItem('Network:DataFolder', datafolder);
    } else {
      this.resetNetworkSelection();
    }
  }
}
