import { Injectable } from '@angular/core';
import { Logger } from './logger.service';

export interface Chain {
  name: string;
  tooltip: string;
  networkname: string;
  port?: number;
  rpcPort?: number;
  apiPort?: number;
  xServerPort?: number;
  wsPort?: number;
  network: string;
  mode?: string;
  genesisDate?: Date;
  path?: string;
  xServerPath?: string;
  datafolder?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ChainService {

  static singletonInstance: ChainService;

  private availableChains: Array<Chain>;

  constructor(private log: Logger) {

    if (!ChainService.singletonInstance) {

      this.availableChains = [
        { name: 'x42', network: 'x42main', tooltip: 'xCore', networkname: 'Main Network', port: 52342, rpcPort: 52343, apiPort: 42220, xServerPort: 4242, wsPort: 42222, genesisDate: new Date(2018, 8, 1) },
        { name: 'x42', network: 'x42test', tooltip: 'xCore (TestNet)', networkname: 'Test Network', port: 62342, rpcPort: 62343, apiPort: 42221, xServerPort: 4243, wsPort: 42223, genesisDate: new Date(2020, 6, 6) }
      ];

      ChainService.singletonInstance = this;
    }

    return ChainService.singletonInstance;
  }

  /** Retrieves a configuration for a blockchain, including the right network name and ports. */
  getChain(network: string = 'x42main'): Chain {
    const selectedChains = this.availableChains.filter(c => c.network === network);
    let selectedChain: Chain;

    if (selectedChains.length === 0) {
      this.log.error('The supplied coin parameter is invalid. First available chain selected as default. Argument value: ' + network);
      selectedChain = this.availableChains[0];
    } else {
      selectedChain = selectedChains[0];
    }

    return selectedChain;
  }
}
