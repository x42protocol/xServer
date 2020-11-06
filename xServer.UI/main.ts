import { app, BrowserWindow, ipcMain, Menu, nativeImage, Tray } from 'electron';
import * as path from 'path';
import * as url from 'url';
import * as os from 'os';
if (os.arch() == 'arm') {
  app.disableHardwareAcceleration();
}

let serve;
let testnet;
let sidechain;
let nodaemon;
const args = process.argv.slice(1);
serve = args.some(val => val === "--serve" || val === "-serve");
testnet = args.some(val => val === "--testnet" || val === "-testnet");
sidechain = args.some(val => val === "--sidechain" || val === "-sidechain");
nodaemon = args.some(val => val === "--nodaemon" || val === "-nodaemon");

let apiPort;
if (testnet) {
  apiPort = 42221;
} else {
  apiPort = 42220;
}

let xServerPort;
if (testnet) {
  xServerPort = 4243;
} else {
  xServerPort = 4242;
}

ipcMain.on('get-port', (event, arg) => {
  event.returnValue = apiPort;
});

ipcMain.on('get-xserver-port', (event, arg) => {
  event.returnValue = xServerPort;
});

ipcMain.on('get-testnet', (event, arg) => {
  event.returnValue = testnet;
});

ipcMain.on('get-sidechain', (event, arg) => {
  event.returnValue = sidechain;
});

require('electron-context-menu')({
  showInspectElement: serve
});


// Keep a global reference of the window object, if you don't, the window will
// be closed automatically when the JavaScript object is garbage collected.
let mainWindow = null;

function createWindow() {
  // Create the browser window.
  mainWindow = new BrowserWindow({
    width: 1150,
    height: 800,
    frame: true,
    minWidth: 1150,
    minHeight: 800,
    title: "x42 Server",
    webPreferences: {
      nodeIntegration: true,
      enableRemoteModule: true
    }
  });

  if (serve) {
    require('electron-reload')(__dirname, {
    });
    mainWindow.loadURL('http://localhost:4200');
  } else {
    mainWindow.loadURL(url.format({
      pathname: path.join(__dirname, 'dist/index.html'),
      protocol: 'file:',
      slashes: true
    }));
  }

  if (serve) {
    mainWindow.webContents.openDevTools();
  }

  // Emitted when the window is going to close.
  mainWindow.on('close', function (e) {
    shutdownx42Node(apiPort);
    shutdownxServer(xServerPort);
  });

  // Emitted when the window is closed.
  mainWindow.on('closed', () => {
    // Dereference the window object, usually you would store window
    // in an array if your app supports multi windows, this is the time
    // when you should delete the corresponding element.
    mainWindow = null;
  });

  // Remove menu, new from Electron 5
  mainWindow.removeMenu();

};

// This method will be called when Electron has finished
// initialization and is ready to create browser windows.
// Some APIs can only be used after this event occurs.
app.on('ready', () => {
  if (serve) {
    console.log("x42 UI was started in development mode. This requires the user to be running the x42 Full Node Daemon themself.")
  }
  else {
    if (sidechain && !nodaemon) {
      startx42Node("x42.Node");
      startxServer("xServer");
    } else if (!nodaemon) {
      startx42Node("x42.Node");
      startxServer("x42.xServerD");
    }
  }
  createTray();
  createWindow();
  if (os.platform() === 'darwin') {
    createMenu();
  }
});

/* 'before-quit' is emitted when Electron receives 
 * the signal to exit and wants to start closing windows */
app.on('before-quit', () => {
  if (!serve && !nodaemon) {
    shutdownx42Node(apiPort);
    shutdownxServer(xServerPort);
  }
});

app.on('quit', () => {
  if (!serve && !nodaemon) {
    shutdownx42Node(apiPort);
    shutdownxServer(xServerPort);
  }
});

// Quit when all windows are closed.
app.on('window-all-closed', () => {
  shutdownx42Node(apiPort);
  shutdownxServer(xServerPort);
  app.quit();
});

app.on('activate', () => {
  // On OS X it's common to re-create a window in the app when the
  // dock icon is clicked and there are no other windows open.
  if (mainWindow === null) {
    createWindow();
  }
});

function shutdownx42Node(portNumber) {
  const http = require('http');
  const options = {
    hostname: 'localhost',
    port: portNumber,
    path: '/api/node/shutdown',
    method: 'POST',
    body: 'true'
  };

  const req = http.request(options);

  req.on('response', function (res) {
    if (res.statusCode === 200) {
      console.log('Request to shutdown x42 node daemon returned HTTP success code.');
    } else {
      console.log('Request to shutdown x42 node daemon returned HTTP failure code: ' + res.statusCode);
    }
  });

  req.on('error', function (err) { });

  req.setHeader('content-type', 'application/json-patch+json');
  req.write('true');
  req.end();
};

function shutdownxServer(portNumber) {
  const http = require('http');
  const options = {
    hostname: 'localhost',
    port: portNumber,
    path: '/shutdown',
    method: 'POST',
    body: 'true'
  };

  const req = http.request(options);

  req.on('response', function (res) {
    if (res.statusCode === 200) {
      console.log('Request to shutdown xServer daemon returned HTTP success code.');
    } else {
      console.log('Request to shutdown xServer daemon returned HTTP failure code: ' + res.statusCode);
    }
  });

  req.on('error', function (err) { });

  req.setHeader('content-type', 'application/json-patch+json');
  req.write('true');
  req.end();
};

function startx42Node(daemonName) {
  var daemonProcess;
  var spawnDaemon = require('child_process').spawn;

  var daemonPath;
  if (os.platform() === 'win32') {
    daemonPath = path.resolve(__dirname, '..\\..\\resources\\daemon\\' + daemonName + '.exe');
  } else if (os.platform() === 'linux') {
    daemonPath = path.resolve(__dirname, '..//..//resources//daemon//' + daemonName);
  } else {
    daemonPath = path.resolve(__dirname, '..//..//resources//daemon//' + daemonName);
  }

  let nodeArguments = args;
  nodeArguments.push("-txindex=1");
  nodeArguments.push("-addressindex=1");

  daemonProcess = spawnDaemon(daemonPath, nodeArguments, {
    detached: true
  });

  daemonProcess.stdout.on('data', (data) => {
    writeLog(`x42: ${data}`);
  });
}

function startxServer(daemonName) {
  var daemonProcess;
  var spawnDaemon = require('child_process').spawn;

  var daemonPath;
  if (os.platform() === 'win32') {
    daemonPath = path.resolve(__dirname, '..\\..\\resources\\xserver.d\\' + daemonName + '.exe');
  } else if (os.platform() === 'linux') {
    daemonPath = path.resolve(__dirname, '..//..//resources//xserver.d//' + daemonName);
  } else {
    daemonPath = path.resolve(__dirname, '..//..//resources//xserver.d//' + daemonName);
  }

  let nodeArguments = args;
  daemonProcess = spawnDaemon(daemonPath, nodeArguments, {
    detached: true
  });

  daemonProcess.stdout.on('data', (data) => {
    writeLog(`x42: ${data}`);
  });
}

function createTray() {
  //Put the app in system tray
  let trayIcon;
  if (serve) {
    trayIcon = nativeImage.createFromPath('./src/assets/images/icons/32x32.png');
  } else {
    trayIcon = nativeImage.createFromPath(path.resolve(__dirname, '../../resources/src/assets/images/icons/32x32.png'));
  }

  let systemTray = new Tray(trayIcon);
  const contextMenu = Menu.buildFromTemplate([
    {
      label: 'Hide/Show',
      click: function () {
        mainWindow.isVisible() ? mainWindow.hide() : mainWindow.show();
      }
    },
    {
      label: 'Exit',
      click: function () {
        app.quit();
      }
    }
  ]);
  systemTray.setToolTip('x42 Server');
  systemTray.setContextMenu(contextMenu);
  systemTray.on('click', function () {
    if (!mainWindow.isVisible()) {
      mainWindow.show();
    }

    if (!mainWindow.isFocused()) {
      mainWindow.focus();
    }
  });

  app.on('window-all-closed', function () {
    shutdownx42Node(apiPort);
    shutdownxServer(xServerPort);
    if (systemTray) systemTray.destroy();
  });
};

function writeLog(msg) {
  console.log(msg);
};

function createMenu() {
  var menuTemplate = [{
    label: app.getName(),
    submenu: [
      { label: "About " + app.getName(), selector: "orderFrontStandardAboutPanel:" },
      { label: "Quit", accelerator: "Command+Q", click: function () { app.quit(); } }
    ]
  }, {
    label: "Edit",
    submenu: [
      { label: "Undo", accelerator: "CmdOrCtrl+Z", selector: "undo:" },
      { label: "Redo", accelerator: "Shift+CmdOrCtrl+Z", selector: "redo:" },
      { label: "Cut", accelerator: "CmdOrCtrl+X", selector: "cut:" },
      { label: "Copy", accelerator: "CmdOrCtrl+C", selector: "copy:" },
      { label: "Paste", accelerator: "CmdOrCtrl+V", selector: "paste:" },
      { label: "Select All", accelerator: "CmdOrCtrl+A", selector: "selectAll:" }
    ]
  }
  ];

  Menu.setApplicationMenu(Menu.buildFromTemplate(menuTemplate));
};
