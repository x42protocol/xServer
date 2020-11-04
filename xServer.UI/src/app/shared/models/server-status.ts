export class ServerStatus {
  constructor(name: string, version: string, processId: number, connectedServers: [Peer], enabledFeatures: [string], dataDirectoryPath: string, runningtime: string, protocolVersion: number, state: string, databaseConnected: boolean, stats: Stats, feeAddress?: any) {
    this.name = name;
    this.feeAddress = feeAddress;
    this.version = version;
    this.processId = processId;
    this.connectedServers = connectedServers;
    this.enabledFeatures = enabledFeatures;
    this.dataDirectoryPath = dataDirectoryPath;
    this.runningTime = runningtime;
    this.protocolVersion = protocolVersion;
    this.state = state;
    this.databaseConnected = databaseConnected;
    this.stats = stats;
  }

  public name: string;
  public feeAddress?: any;
  public version: string;
  public processId: number;
  public connectedServers: [Peer];
  public enabledFeatures: [string];
  public dataDirectoryPath: string;
  public runningTime: string;
  public protocolVersion: number;
  public state: string;
  public databaseConnected: boolean;
  public stats: Stats;
}

class Peer {
  constructor(version, remoteSocketEndpoint, tipHeight) {
    this.version = version;
    this.remoteSocketEndpoint = remoteSocketEndpoint;
    this.tipHeight = tipHeight;
  }

  public version: string;
  public remoteSocketEndpoint: string;
  public tipHeight: number;
}

class Stats {
  startupState: number;
  blockHeight: number;
  addressIndexerHeight: number;
  publicRequestCount: number;
  sessionRunTimeSeconds: number;
  state: number;
  tierLevel: number;
}
