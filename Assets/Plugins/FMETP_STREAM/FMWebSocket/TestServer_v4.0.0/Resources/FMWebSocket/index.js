//-- SECTION BEGIN: Create Server --
//ref: https://programmer.group/in-nodejs-http-protocol-and-ws-protocol-reuse-the-same-port.html
var express = require("express");
const http = require("http");//upgrade port for http use...
const WS_MODULE = require("ws");

const app = express();
app.use(express.static(__dirname + '/public'));
const port = 3000;

app.get("/hello", (req, res) => { res.send("hello world"); });

const server = http.createServer(app);
ws = new WS_MODULE.Server({server});
server.listen(port, () => { console.log("++ Server turned on, port number:" + port); });

// Initialize WebSocket connection handling
const { clients, rooms, uuidv4, ByteToInt32, ByteToInt16, initializeWebSocketHandling } = require('./core');
initializeWebSocketHandling(ws);
//-- SECTION END: Create Server --
