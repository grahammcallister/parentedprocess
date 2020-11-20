// Modules to control application life and create native browser window
const {app, BrowserWindow} = require('electron')
const path = require('path')
const child_process = require('child_process');
const ps = require('ps-node');

function createWindow () {
  // Create the browser window.
  const mainWindow = new BrowserWindow({
    width: 800,
    height: 600,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      nodeIntegration: true
    }
  })

  // and load the index.html of the app.
  mainWindow.loadFile('index.html')

  // Open the DevTools.
  //mainWindow.webContents.openDevTools()

  var processToParent = 'C:\\Windows\\notepad.exe';

  var parentExe = 'C:\\Projects\\GitHub\\parentedprocess\\Parent\\bin\\Debug\\Parent.exe'

  var handle = getNativeWindowHandleInt(mainWindow.getNativeWindowHandle());

  var args = [`-p=1234-12345-12345-00000006`, `-c=${processToParent}`, `-h=${handle}`];
  
  var theSpawn = child_process.spawn(
    parentExe,
    args,
    { detached : true }
  );

  var net = require('net');

var PIPE_PATH = "\\\\.\\pipe\\1234-12345-12345-00000006_server";
var PIPE_PATH_CLIENT = "\\\\.\\pipe\\1234-12345-12345-00000006_client";

var server = net.createServer(function(stream) {
  console.log('Server: on connection');

  stream.on('data', function(c) {
      console.log('Server: on data:', c.toString());
  });

  stream.on('end', function() {
      console.log('Server: on end')
      server.close();
  });

  mainWindow.on('resize', function () {
    var size   = mainWindow.getSize();
    var width  = size[0];
    var height = size[1];
    var message = JSON.stringify({ ...size, 'eventName': 'resize'});
    console.log(message);
    stream.write(message);
  });
});

server.on('close',function(){
  console.log('Server: on close');
})

server.listen(PIPE_PATH_CLIENT,function(){
  console.log('Server: on listening');
})



var clientConnect;
var clientConnected = false;

var tryConnect = function() {
  if(!clientConnected) {
  try {
      var client = net.connect(PIPE_PATH, function() {
        console.log('Client: on connection');
        clientConnected = true;
      });
      
      client.on('data', function(data) {
        console.log('Client: on data:', data.toString());
      });
      
      client.on('end', function() {
        console.log('Client: on end');
        client.end();
        clientConnected = false;
      });
      
      client.on('connect', function() { console.log('Connected'); clientConnected = true; });
      
  } catch (Exception){}
}
};

var clientConnectInterval = setInterval(tryConnect, 1500);

  app.on('before-quit', function() {
    if(theSpawn) {
      ps.kill(theSpawn.pid);
    }
    if(client){
      client.end();
    }
  });

  // exec(toExec, (error, stdout, stderr) => {
  //   if (error) {
  //     console.error(`error: ${error.message}`);
  //     return;
  //   }

  //   if (stderr) {
  //     console.error(`stderr: ${stderr}`);
  //     return;
  //   }

  //   console.log(`stdout:\n${stdout}`);
  //   });
}

function getNativeWindowHandleInt(buffer) {
  var os = require('os');
    if (os.endianness() == "LE") {
      return buffer.readInt32LE()
  }
  else {
      return buffer.readInt32BE()
  }
}

// This method will be called when Electron has finished
// initialization and is ready to create browser windows.
// Some APIs can only be used after this event occurs.
app.whenReady().then(() => {
  createWindow()
  
  app.on('activate', function () {
    // On macOS it's common to re-create a window in the app when the
    // dock icon is clicked and there are no other windows open.
    if (BrowserWindow.getAllWindows().length === 0) createWindow()
  })
})

// Quit when all windows are closed, except on macOS. There, it's common
// for applications and their menu bar to stay active until the user quits
// explicitly with Cmd + Q.
app.on('window-all-closed', function () {
  if (process.platform !== 'darwin') app.quit()
})

// In this file you can include the rest of your app's specific main process
// code. You can also put them in separate files and require them here.

