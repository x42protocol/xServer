export class ServerStatus {
  constructor(version: string, processId: number, connectedServers: [Peer], enabledFeatures: [string], dataDirectoryPath: string, runningtime: string, protocolVersion: number, state: string, databaseConnected: boolean) {
    this.version = version;
    this.processId = processId;
    this.connectedServers = connectedServers;
    this.enabledFeatures = enabledFeatures;
    this.dataDirectoryPath = dataDirectoryPath;
    this.runningTime = runningtime;
    this.protocolVersion = protocolVersion;
    this.state = state;
    this.databaseConnected = databaseConnected;
  }
  
  public version: string;
  public processId: number;
  public connectedServers: [Peer];
  public enabledFeatures: [string];
  public dataDirectoryPath: string;
  public runningTime: string;
  public protocolVersion: number;
  public state: string;
  public databaseConnected: boolean;
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
