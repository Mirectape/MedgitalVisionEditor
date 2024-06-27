//-- SECTION BEGIN: SSL setup --
/* Load SSL certificate and key
cmd: openssl genpkey -algorithm RSA -out private.key -aes256
cmd: openssl req -new -key private.key -out csr.pem
cmd: openssl req -x509 -days 365 -key private.key -in csr.pem -out certificate.crt
*/
const fs = require('fs');
const privateKey = fs.readFileSync(__dirname  +"/ssl/private.key"); //relative path to this js folder
const passphrase = 'password'; // Enter your passphrase here
const certificate = fs.readFileSync(__dirname +"/ssl/certificate.crt"); //relative path to this js folder

// Get the SSL context option constants
const constants = require('constants');
const secureOptions = constants.SSL_OP_NO_TLSv1 | constants.SSL_OP_NO_TLSv1_1; // Use TLS1.2 // Adjust the this line to specify the desired SSL/TLS versions
const options =
{
  key: privateKey,
  cert: certificate,
  passphrase: passphrase,
  secureOptions: secureOptions
};
//-- SECTION END: SSL setup --

//-- SECTION BEGIN: Create Server --
//ref: https://programmer.group/in-nodejs-http-protocol-and-ws-protocol-reuse-the-same-port.html
var express = require("express");
const https = require("https");//upgrade port for https use...
const WS_MODULE = require("ws");

const app = express();
app.use(express.static(__dirname + '/public'));
const port = 3000;

app.get("/hello", (req, res) => { res.send("hello world"); });

// Create HTTPS server with SSL options
const server = https.createServer(options, app);
ws = new WS_MODULE.Server({server});
server.listen(port, () => { console.log("++ Server turned on, port number:" + port); });

// Initialize WebSocket connection handling
const { clients, rooms, uuidv4, ByteToInt32, ByteToInt16, initializeWebSocketHandling } = require('./core');
initializeWebSocketHandling(ws);
//-- SECTION END: Create Server --
