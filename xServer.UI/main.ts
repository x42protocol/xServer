import { app, BrowserWindow, ipcMain, Menu, nativeImage, screen, Tray, shell, dialog } from 'electron';
import * as path from 'path';
import * as url from 'url';
import * as os from 'os';

const log = require('electron-log');
const { autoUpdater } = require('electron-updater');
const fs = require('fs');
const readChunk = require('read-chunk');

// Set the log level to info. This is only for logging in this Electron main process.
log.transports.file.level = 'info';

const gotTheLock = app.requestSingleInstanceLock();

if (!gotTheLock) {
  writeLog('Another instance of node is running. Quitting this instance.');
  app.quit();
} else {
  app.on('second-instance', (event, commandLine, workingDirectory) => {
    writeLog('Another instance of node is running. Attempting to show the UI.');

    // Someone tried to run a second instance, we should focus our window.
    if (mainWindow) {
      // If not visible, ensure we show it.
      if (!mainWindow.isVisible()) {
        mainWindow.show();
      }

      // If minimized, ensure we restore it.
      if (mainWindow.isMinimized()) {
        mainWindow.restore();
      }

      // Eventually put focus on the window.
      mainWindow.focus();
    }
  });
}

if (os.arch() === 'arm') {
  writeLog('ARM: Disabling hardware acceleration.');
  app.disableHardwareAcceleration();
}

enum DaemonState {
  Unknown = 0,
  Starting = 1,
  Started = 2,
  Changing = 3,
  Stopping = 4,
  Stopped = 5,
  Failed = 6
}

interface Chain {
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

interface Settings {
  openAtLogin: boolean;
  showInTaskbar: boolean;
}

// We don't want to support auto download.
autoUpdater.autoDownload = false;

// Keep a global reference of the window object, if you don't, the window will
// be closed automatically when the JavaScript object is garbage collected.
let mainWindow = null;
let daemonState: DaemonState;
let xServerDaemonState: DaemonState;
let contents = null;
let currentNetwork: Chain;
let settings: Settings;

const args = process.argv.slice(1);
const serve = args.some(val => val === '--serve');
const coin = { identity: 'x42', tooltip: 'xServer' };

require('electron-context-menu')({
  showInspectElement: serve
});

process.on('uncaughtException', (error) => {
  writeLog('Uncaught exception happened:');
  writeLog('Error: ' + error);
});

ipcMain.on('start-daemon', (event, arg: Chain) => {
  if (daemonState === DaemonState.Started) {
    writeLog('Main process was instructed to start daemon, but is is already running. Ignoring request.');
    event.returnValue = 'OK';
    return;
  }

  daemonState = DaemonState.Starting;

  console.log(arg);

  // The "chain" object is supplied over the IPC channel and we should consider
  // it potentially "hostile", if anyone can inject anything in the app and perform
  // a call to the node backend here. Since we are launching a process here,
  // we should make sure to wash and validate the object properly to make it
  // harder to perform a remote execution exploit through this interface.
  assert(isNumber(arg.port));
  assert(isNumber(arg.rpcPort));
  assert(isNumber(arg.apiPort));
  assert(isNumber(arg.wsPort));
  assert(arg.network.length < 20);

  currentNetwork = arg;

  writeLog(currentNetwork);

  if (arg.mode === 'manual') {
    daemonState = DaemonState.Started;
    const msg = 'xServer was started in development mode. This requires the user to be running the daemon manually.';
    writeLog(msg);
    event.returnValue = msg;
  } else {
    startDaemon(currentNetwork);
    event.returnValue = 'OK';
  }
});

ipcMain.on('start-xserver-daemon', (event, arg: Chain) => {
  if (xServerDaemonState === DaemonState.Started) {
    writeLog('Main process was instructed to start xServer daemon, but is is already running. Ignoring request.');
    event.returnValue = 'OK';
    return;
  }

  xServerDaemonState = DaemonState.Starting;

  console.log(arg);

  // The "chain" object is supplied over the IPC channel and we should consider
  // it potentially "hostile", if anyone can inject anything in the app and perform
  // a call to the node backend here. Since we are launching a process here,
  // we should make sure to wash and validate the object properly to make it
  // harder to perform a remote execution exploit through this interface.
  assert(isNumber(arg.xServerPort));
  assert(arg.network.length < 20);

  currentNetwork = arg;

  writeLog(currentNetwork);

  if (arg.mode === 'manual') {
    xServerDaemonState = DaemonState.Started;
    const msg = 'xServer was started in development mode. This requires the user to be running the daemon and xserver.d manually.';
    writeLog(msg);
    event.returnValue = msg;
  } else {
    startXServerDaemon(currentNetwork);
    event.returnValue = 'OK';
  }
});

ipcMain.on('settings', (event, arg: Settings) => {
  // Update the global settings for the Main thread.
  settings = arg;

  app.setLoginItemSettings({
    openAtLogin: arg.openAtLogin
  });
});

ipcMain.on('check-for-update', (event, arg: Chain) => {
  autoUpdater.checkForUpdates();
});

ipcMain.on('download-update', (event, arg: Chain) => {
  autoUpdater.downloadUpdate();
});

ipcMain.on('install-update', (event, arg: Chain) => {
  autoUpdater.quitAndInstall();
});

ipcMain.on('daemon-started', (event, arg: Chain) => {
  daemonState = DaemonState.Started;
  xServerDaemonState = DaemonState.Started;
});

ipcMain.on('daemon-change', (event, arg: any) => {
  daemonState = DaemonState.Changing;
  xServerDaemonState = DaemonState.Changing;
});

// Called when the app needs to reset the blockchain database. It will delete the "blocks", "chain" and "coinview", etc folders.
ipcMain.on('reset-database', (event, arg: string) => {

  writeLog('reset-database: User wants to reset database, first attempting to shutdown the node.');

  // Make sure the daemon is shut down first:
  shutdownDaemon((success, error) => {
    const userDataPath = app.getPath('userData');
    const appDataFolder = path.dirname(userDataPath);

    const dataFolder = path.join(appDataFolder, 'x42', arg);
    const folderBlocks = path.join(dataFolder, 'blocks');
    const folderChain = path.join(dataFolder, 'chain');
    const folderCoinView = path.join(dataFolder, 'coinview');
    const folderCommon = path.join(dataFolder, 'common');
    const folderProvenHeaders = path.join(dataFolder, 'provenheaders');
    const folderFinalizedBlock = path.join(dataFolder, 'finalizedBlock');
    const txdb = path.join(dataFolder, 'txdb');

    // After shutdown completes, we'll delete the database.
    deleteFolderRecursive(folderBlocks);
    deleteFolderRecursive(folderChain);
    deleteFolderRecursive(folderCoinView);
    deleteFolderRecursive(folderCommon);
    deleteFolderRecursive(folderProvenHeaders);
    deleteFolderRecursive(folderFinalizedBlock);
    deleteFolderRecursive(txdb);
  });

  event.returnValue = 'OK';
});

ipcMain.on('open-data-folder', (event, arg: string) => {
  const userDataPath = app.getPath('userData');
  const appDataFolder = path.dirname(userDataPath);
  const dataFolder = path.join(appDataFolder, 'x42', arg);
  shell.openPath(dataFolder);

  event.returnValue = 'OK';
});

ipcMain.on('open-dev-tools', (event, arg: string) => {
  mainWindow.webContents.openDevTools();
  event.returnValue = 'OK';
});

ipcMain.on('get-wallet-seed', (event, arg: string) => {

  writeLog('get-wallet-seed: Send the encrypted seed and chain code to the UI.');

  // TODO: Consider doing this async to avoid UI hanging, but to simplify the integration at the moment and
  // use return value, we rely on sync read.  "readChunk(filePath, startPosition, length)" <- async
  // Read 300 characters, that should be more than enough to get the encryptedSeed. Consider doing a loop until we find it.
  const dataBuffer = readChunk.sync(arg, 1, 500);
  const data = dataBuffer.toString('utf8');

  const key = '"encryptedSeed":"';
  const startIndex = data.indexOf(key);
  const endIndex = data.indexOf('",', startIndex);
  const seed = data.substring(startIndex + key.length, endIndex);

  const keyChainCode = '"chainCode":"';
  const startIndexChainCode = data.indexOf(keyChainCode);
  const endIndexChainCode = data.indexOf('",', startIndexChainCode);
  const chainCode = data.substring(startIndexChainCode + keyChainCode.length, endIndexChainCode);

  // chainCodeDecoded: Buffer.from(chainCode, 'base64')
  event.returnValue = { encryptedSeed: seed, chainCode };
});

autoUpdater.on('checking-for-update', () => {
  if (!serve) {
    contents.send('checking-for-update');
    writeLog('Checking for update...');
  }
});

autoUpdater.on('error', (error) => {
  contents.send('update-error', error);
});

autoUpdater.on('update-available', (info) => {
  contents.send('update-available', info);
});

autoUpdater.on('update-not-available', (info) => {
  contents.send('update-not-available', info);
});

autoUpdater.on('update-downloaded', (info) => {
  contents.send('update-downloaded', info);
});

autoUpdater.on('download-progress', (progressObj) => {
  contents.send('download-progress', progressObj);

  let log_message = 'Download speed: ' + progressObj.bytesPerSecond;
  log_message = log_message + ' - Downloaded ' + progressObj.percent + '%';
  log_message = log_message + ' (' + progressObj.transferred + '/' + progressObj.total + ')';
  writeLog(log_message);
});

function deleteFolderRecursive(folder) {
  if (fs.existsSync(folder)) {
    fs.readdirSync(folder).forEach(function (file, index) {
      const curPath = folder + '/' + file;
      if (fs.lstatSync(curPath).isDirectory()) { // recurse
        deleteFolderRecursive(curPath);
      } else { // delete file
        fs.unlinkSync(curPath);
      }
    });
    fs.rmdirSync(folder);
  }
}

function createWindow() {
  // Create the browser window.
  mainWindow = new BrowserWindow({
    width: 1150,
    height: 850,
    frame: true,
    minWidth: 1150,
    minHeight: 850,
    title: "x42 Server",
    webPreferences: { webSecurity: false, nodeIntegration: true, enableRemoteModule: true }
  });

  contents = mainWindow.webContents;

  mainWindow.setMenu(null);

  // Make sure links that open new window, e.g. target="_blank" launches in external window (browser).
  mainWindow.webContents.on('new-window', function (event, linkUrl) {
    event.preventDefault();
    shell.openExternal(linkUrl);
  });

  if (serve) {
    mainWindow.webContents.openDevTools();

    require('electron-reload')(__dirname, {
      electron: require(`${__dirname}/node_modules/electron`)
    });

    writeLog('Creating Window and loading: http://localhost:4200?coin=' + coin.identity);
    mainWindow.loadURL('http://localhost:4200?coin=' + coin.identity);
  } else {
    writeLog('Creating Window and loading: ' + path.join(__dirname, 'dist/index.html'));
    mainWindow.loadURL(url.format({
      pathname: path.join(__dirname, 'dist/index.html'),
      protocol: 'file:',
      slashes: true
    }));
  }

  // Emitted when the window is going to close.
  mainWindow.on('close', (event) => {
    writeLog(`close event on mainWindow was triggered. Calling shutdown method. x42.Node state is: ${daemonState} and xServer Daemon state is: ${xServerDaemonState}.`);

    let result = false;

    // If daemon stopping has not been triggered, it means it likely never started and user clicked Exit on the error dialog. Exit immediately.
    // Additionally if it was never started, it is already stopped.
    if (xServerDaemonState === DaemonState.Stopping || xServerDaemonState === DaemonState.Stopped) {
      writeLog('xServer Daemon was in stopping mode, so exiting immediately without showing status any longer.');
      result = true;
    } else {
      // If shutdown not initated yet, perform it.
      if (xServerDaemonState === DaemonState.Started) {
        writeLog('xServer Daemon shutdown initiated... preventing window close, and informing UI that shutdown is in progress.');

        xServerDaemonState = DaemonState.Stopping;

        event.preventDefault();

        contents.send('xserver-daemon-exiting');

        // Call the shutdown while we show progress window.
        shutdownXServer(() => { });

        result = true;
      } else { // Else, allow window to be closed. This allows users to click X twice to immediately close the window.
        writeLog('ELSE in the CLOSE event. Should only happen on double-click on exit button.');
      }
    }

    if (daemonState === DaemonState.Stopping || daemonState === DaemonState.Stopped) {
      writeLog('Daemon was in stopping mode, so exiting immediately without showing status any longer.');
      result = true;
    } else {
      // If shutdown not initated yet, perform it.
      if (daemonState === DaemonState.Started) {
        writeLog('Daemon shutdown initiated... preventing window close, and informing UI that shutdown is in progress.');

        daemonState = DaemonState.Stopping;

        event.preventDefault();

        contents.send('daemon-exiting');

        // Call the shutdown while we show progress window.
        shutdown(() => { });

        result = true;
      } else { // Else, allow window to be closed. This allows users to click X twice to immediately close the window.
        writeLog('ELSE in the CLOSE event. Should only happen on double-click on exit button.');
      }
    }

    if (result) {
      return true;
    }

  });

  mainWindow.on('minimize', (event) => {
    if (!settings.showInTaskbar) {
      event.preventDefault();
      mainWindow.hide();
    }
  });

  // Emitted when the window is closed.
  mainWindow.on('closed', () => {
    // Dereference the window object, usually you would store window
    // in an array if your app supports multi windows, this is the time
    // when you should delete the corresponding element.
    mainWindow = null;
  });
}

// This method will be called when Electron has finished
// initialization and is ready to create browser windows.
// Some APIs can only be used after this event occurs.
app.on('ready', () => {
  createTray();
  createWindow();
});

app.on('before-quit', () => {
  writeLog('xServer was exited.');
});

const shutdown = (callback) => {
  writeLog('Signal a shutdown to the daemons.');
  shutdownDaemon((success, error) => {
    if (success) {
      writeLog('Shutdown daemon signaling completed. Waiting for exit signal.');
      callback();
    } else {
      writeError('Shutdown daemon signaling failed. Attempting a single retry.');
      writeError(error);
      // Perform another retry, and quit no matter the result.
      shutdownDaemon((ok, err) => {
        if (ok) {
          writeLog('Shutdown daemon retry signaling completed successfully.');
        } else {
          writeError('Shutdown daemon retry signaling failed.');
          writeError(err);
        }

        // Inform that we are unable to shutdown the daemon.
        contents.send('daemon-exited', { message: 'Unable to communicate with background process.' });

        callback();
      });
    }
  });
};
const shutdownXServer = (callback) => {
  shutdownXServerDaemon((success, error) => {
    if (success) {
      writeLog('Shutdown xServer daemon signaling completed. Waiting for exit signal.');
      callback();
    } else {
      writeError('Shutdown xServer daemon signaling failed. Attempting a single retry.');
      writeError(error);
      // Perform another retry, and quit no matter the result.
      shutdownXServerDaemon((ok, err) => {
        if (ok) {
          writeLog('Shutdown xServer daemon retry signaling completed successfully.');
        } else {
          writeError('Shutdown xServer daemon retry signaling failed.');
          writeError(err);
        }

        // Inform that we are unable to shutdown the daemon.
        contents.send('xserver-daemon-exited', { message: 'Unable to communicate with xServer background process.' });

        callback();
      });
    }
  });
}

const quit = () => {
  app.quit();
};

app.on('window-all-closed', () => {
  quit();
});

app.on('activate', () => {
  // On OS X it's common to re-create a window in the app when the
  // dock icon is clicked and there are no other windows open.
  if (mainWindow === null) {
    createWindow();
  }
});

function startDaemon(chain: Chain) {
  const folderPath = chain.path || getDaemonPath();
  let daemonName;

  if (chain.name === 'x42') {
    daemonName = 'x42.Node';
  }

  // If path is not specified and Win32, we'll append .exe
  if (!chain.path && os.platform() === 'win32') {
    daemonName += '.exe';
  } else if (chain.path) {
    daemonName += '.dll';
  }

  const daemonPath = path.resolve(folderPath, daemonName);

  writeLog('start-daemon: ' + daemonPath);

  launchDaemon(daemonPath, chain);
}

function getDaemonPath() {
  let apiPath;
  if (os.platform() === 'win32') {
    apiPath = path.resolve(__dirname, '..\\..\\resources\\daemon\\');
  } else if (os.platform() === 'linux') {
    apiPath = path.resolve(__dirname, '..//..//resources//daemon//');
  } else {
    apiPath = path.resolve(__dirname, '..//..//resources//daemon//');
  }

  return apiPath;
}

function startXServerDaemon(chain: Chain) {
  const xServerfolderPath = chain.xServerPath || getxServerDaemonPath();
  let xServerDaemonName;

  if (chain.name === 'x42') {
    xServerDaemonName = 'x42.xServerD';
  }

  // If path is not specified and Win32, we'll append .exe
  if (!chain.path && os.platform() === 'win32') {
    xServerDaemonName += '.exe';
  } else if (chain.path) {
    xServerDaemonName += '.dll';
  }

  const xServerDaemonPath = path.resolve(xServerfolderPath, xServerDaemonName);

  writeLog('start-xserver-daemon: ' + xServerDaemonPath);

  launchxServerDaemon(xServerDaemonPath, chain);
}

function getxServerDaemonPath() {
  let apiPath;
  if (os.platform() === 'win32') {
    apiPath = path.resolve(__dirname, '..\\..\\resources\\xserver.d\\');
  } else if (os.platform() === 'linux') {
    apiPath = path.resolve(__dirname, '..//..//resources//xserver.d//');
  } else {
    apiPath = path.resolve(__dirname, '..//..//resources//xserver.d//');
  }

  return apiPath;
}

function launchDaemon(apiPath: string, chain: Chain) {
  let daemonProcess;

  // TODO: Consider a future improvement that would ensure we don't loose a reference to an existing spawned process.
  // If launch is called twice, it might spawn two processes and loose the reference to the first one, and the new process will die due to TCP port lock.
  const spawnDaemon = require('child_process').spawn;

  const commandLineArguments = [];

  if (chain.mode === 'local') {
    if (!apiPath || apiPath.length < 3 || !chain.datafolder || chain.datafolder.length < 3) {
      contents.send('daemon-error', `CRITICAL: Cannot launch daemon, missing either daemon path or data folder path.`);
      daemonState = DaemonState.Failed;
      return;
    }

    // Only append the apiPath as argument if we are in local mode.
    commandLineArguments.push(apiPath);
  }

  if (chain.datafolder) {
    commandLineArguments.push('-datadir=' + chain.datafolder);
  }

  commandLineArguments.push('-port=' + chain.port);
  commandLineArguments.push('-rpcport=' + chain.rpcPort);
  commandLineArguments.push('-apiport=' + chain.apiPort);
  commandLineArguments.push('-wsport=' + chain.wsPort);
  commandLineArguments.push("-txindex=1");
  commandLineArguments.push("-addressindex=1");

  if (chain.mode === 'light') {
    commandLineArguments.push('-light');
  }

  if (chain.network.indexOf('regtest') > -1) {
    commandLineArguments.push('-regtest');
  } else if (chain.network.indexOf('test') > -1) {
    commandLineArguments.push('-testnet');
  }

  writeLog('LAUNCH: ' + apiPath);
  writeLog('ARGS: ' + JSON.stringify(commandLineArguments));

  // TODO: Consider adding an advanced option in the setup dialog, to allow a custom datadir folder.
  // if (chain.dataDir != null)
  // commandLineArguments.push("-datadir=" + chain.dataDir);

  writeLog('Starting daemon with parameters: ' + commandLineArguments);

  if (chain.mode === 'local') {
    daemonProcess = spawnDaemon('dotnet', commandLineArguments, {
      detached: true
    });
  } else {
    daemonProcess = spawnDaemon(apiPath, commandLineArguments, {
      detached: true
    });
  }

  daemonProcess.stdout.on('data', (data) => {
    writeDebug(`x42: ${data}`);
  });

  /** Exit is triggered when the process exits. */
  daemonProcess.on('exit', function (code, signal) {
    writeLog(`x42 Node process exited with code ${code} and signal ${signal} when the state was ${daemonState}.`);

    // There are many reasons why the daemon process can exit, we'll show details
    // in those cases we get an unexpected shutdown code and signal.
    if (daemonState === DaemonState.Changing) {
      writeLog('Daemon exit was expected, the user is changing the network mode.');
    } else if (daemonState === DaemonState.Starting) {
      contents.send('daemon-error', `CRITICAL: x42 Node process exited during startup with code ${code} and signal ${signal}.`);
    } else if (daemonState === DaemonState.Started) {
      contents.send('daemon-error', `x42 Node process exited manually or crashed, with code ${code} and signal ${signal}.`);
    } else {
      // This is a normal shutdown scenario, but we'll show error dialog if the exit code was not 0 (OK).   
      if (code !== 0) {
        contents.send('daemon-error', `x42 Node shutdown completed, but resulted in exit code ${code} and signal ${signal}.`);
      } else {
        // Check is stopping of daemon has been requested. If so, we'll notify the UI that it has completed the exit.
        contents.send('daemon-exited');
      }
    }

    daemonState = DaemonState.Stopped;
  }
  );

  daemonProcess.on('error', (code, signal) => {
    writeError(`x42 Node process failed to start. Code ${code} and signal ${signal}.`);
  });
}

function launchxServerDaemon(apiPath: string, chain: Chain) {
  let daemonxServerProcess;

  // TODO: Consider a future improvement that would ensure we don't loose a reference to an existing spawned process.
  // If launch is called twice, it might spawn two processes and loose the reference to the first one, and the new process will die due to TCP port lock.
  const spawnxServerDaemon = require('child_process').spawn;

  const commandLineArguments = [];

  if (chain.mode === 'local') {
    if (!apiPath || apiPath.length < 3 || !chain.datafolder || chain.datafolder.length < 3) {
      contents.send('xserver-daemon-error', `CRITICAL: Cannot launch xserver daemon, missing either xserver daemon path or data folder path.`);
      xServerDaemonState = DaemonState.Failed;
      return;
    }

    // Only append the apiPath as argument if we are in local mode.
    commandLineArguments.push(apiPath);
  }

  if (chain.datafolder) {
    commandLineArguments.push('-datadir=' + chain.datafolder);
  }

  if (chain.network.indexOf('regtest') > -1) {
    commandLineArguments.push('-regtest');
  } else if (chain.network.indexOf('test') > -1) {
    commandLineArguments.push('-testnet');
  }

  writeLog('LAUNCH: ' + apiPath);
  writeLog('ARGS: ' + JSON.stringify(commandLineArguments));

  // TODO: Consider adding an advanced option in the setup dialog, to allow a custom datadir folder.
  // if (chain.dataDir != null)
  // commandLineArguments.push("-datadir=" + chain.dataDir);

  writeLog('Starting xServer daemon with parameters: ' + commandLineArguments);

  if (chain.mode === 'local') {
    daemonxServerProcess = spawnxServerDaemon('dotnet', commandLineArguments, {
      detached: true
    });
  } else {
    daemonxServerProcess = spawnxServerDaemon(apiPath, commandLineArguments, {
      detached: true
    });
  }

  daemonxServerProcess.stdout.on('data', (data) => {
    writeDebug(`x42 xServer: ${data}`);
  });

  /** Exit is triggered when the process exits. */
  daemonxServerProcess.on('exit', function (code, signal) {
    writeLog(`x42 xServer process exited with code ${code} and signal ${signal} when the state was ${xServerDaemonState}.`);

    // There are many reasons why the daemon process can exit, we'll show details
    // in those cases we get an unexpected shutdown code and signal.
    if (xServerDaemonState === DaemonState.Changing) {
      writeLog('xServer daemon exit was expected, the user is changing the network mode.');
    } else if (xServerDaemonState === DaemonState.Starting) {
      contents.send('xserver-daemon-error', `CRITICAL: x42 xServer process exited during startup with code ${code} and signal ${signal}.`);
    } else if (xServerDaemonState === DaemonState.Started) {
      contents.send('xserver-daemon-error', `x42 xServer process exited manually or crashed, with code ${code} and signal ${signal}.`);
    } else {
      // This is a normal shutdown scenario, but we'll show error dialog if the exit code was not 0 (OK).   
      if (code !== 0) {
        contents.send('xserver-daemon-error', `x42 xServer shutdown completed, but resulted in exit code ${code} and signal ${signal}.`);
      } else {
        // Check is stopping of daemon has been requested. If so, we'll notify the UI that it has completed the exit.
        contents.send('xserver-daemon-exited');
      }
    }

    xServerDaemonState = DaemonState.Stopped;
  });

  daemonxServerProcess.on('error', (code, signal) => {
    writeError(`x42 Node process failed to start. Code ${code} and signal ${signal}.`);
  });
}

function shutdownDaemon(callback) {

  daemonState = DaemonState.Stopping;

  if (!currentNetwork) {
    writeLog('Network not selected, nothing to shutdown.');
    callback(true, null);
    return;
  }

  if (process.platform !== 'darwin') {
    writeLog('Sending POST request to shut down daemon.');

    const http = require('http');
    const options = {
      hostname: 'localhost',
      port: currentNetwork.apiPort,
      path: '/api/node/shutdown',
      method: 'POST'
    };

    const req = http.request(options);

    req.on('response', (res) => {
      if (res.statusCode === 200) {
        writeLog('Request to shutdown daemon returned HTTP success code.');
        callback(true, null);
      } else {
        writeError('Request to shutdown daemon returned HTTP failure code: ' + res.statusCode);
        callback(false, res);
      }
    });

    req.on('error', (err) => {
      writeError('Request to shutdown daemon failed.');
      callback(false, err);
    });

    req.setHeader('content-type', 'application/json-patch+json');
    req.write('true');
    req.end();
  }
}

function shutdownXServerDaemon(callback) {

  xServerDaemonState = DaemonState.Stopping;

  if (!currentNetwork) {
    writeLog('Network not selected, nothing to shutdown.');
    callback(true, null);
    return;
  }

  if (process.platform !== 'darwin') {
    writeLog('Sending POST request to shut down xServer daemon.');

    const http = require('http');
    const options = {
      hostname: 'localhost',
      port: currentNetwork.xServerPort,
      path: '/shutdown',
      method: 'POST'
    };

    const req = http.request(options);

    req.on('response', (res) => {
      if (res.statusCode === 200) {
        writeLog('Request to shutdown xServer daemon returned HTTP success code.');
        callback(true, null);
      } else {
        writeError('Request to shutdown xServer daemon returned HTTP failure code: ' + res.statusCode);
        callback(false, res);
      }
    });

    req.on('error', (err) => {
      writeError('Request to shutdown xServer daemon failed.');
      callback(false, err);
    });

    req.setHeader('content-type', 'application/json-patch+json');
    req.write('true');
    req.end();
  }
}

function createTray() {
  // Put the app in system tray
  let trayIcon;
  if (serve) {
    trayIcon = nativeImage.createFromPath('./src/assets/images/icons/32x32.png');
  } else {
    trayIcon = nativeImage.createFromPath(path.resolve(__dirname, '../../resources/src/assets/images/icons/32x32.png'));
  }

  const systemTray = new Tray(trayIcon);

  const contextMenu = Menu.buildFromTemplate([
    {
      label: 'Hide/Show',
      click: () => {
        mainWindow.isVisible() ? mainWindow.hide() : mainWindow.show();
      }
    },
    {
      label: 'Exit',
      click: () => {
        mainWindow.close();
      }
    }
  ]);

  systemTray.setToolTip(coin.tooltip);
  systemTray.setContextMenu(contextMenu);

  systemTray.on('click', () => {
    if (!mainWindow.isVisible()) {
      mainWindow.show();
    }

    if (!mainWindow.isFocused()) {
      mainWindow.focus();
    }
  });

  app.on('window-all-closed', () => {
    if (systemTray) {
      systemTray.destroy();
    }
  });
}

function writeDebug(msg) {
  log.debug(msg);

  if (contents) {
    contents.send('log-debug', msg);
  }
}

function writeLog(msg) {
  log.info(msg);

  if (contents) {
    contents.send('log-info', msg);
  }
}

function writeError(msg) {
  log.error(msg);

  if (contents) {
    contents.send('log-error', msg);
  }
}

function isNumber(value: string | number): boolean {
  return !isNaN(Number(value.toString()));
}

function assert(result: boolean) {
  if (result !== true) {
    throw new Error('The network configuration is invalid. Unable to continue.');
  }
}
