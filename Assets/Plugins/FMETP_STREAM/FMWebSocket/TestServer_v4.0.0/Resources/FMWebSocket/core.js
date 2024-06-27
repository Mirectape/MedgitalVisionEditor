const clients = new Map();
const rooms = new Map();
function uuidv4()
{
  function s4() { return Math.floor((1 + Math.random()) * 0x10000).toString(16).substring(1); }
  return s4() + s4() + '-' + s4();
}
function ByteToInt32(_byte, _offset) { return (_byte[_offset] & 255) + ((_byte[_offset + 1] & 255) << 8) + ((_byte[_offset + 2] & 255) << 16) + ((_byte[_offset + 3] & 255) << 24); }
function ByteToInt16(_byte, _offset) { return (_byte[_offset] & 255) + ((_byte[_offset + 1] & 255) << 8); }
function initializeWebSocketHandling(ws)
{
    console.log("++ initialize WebSocket Handling...")
    ws.on('connection', function connection(ws)
    {
        const wsid = uuidv4();
        const networkType = 'undefined';
        const roomName = 'Lobby';
        const metadata = { ws, wsid, networkType, roomName };

        const roomClients = new Map();
        const roomMasterWSID = '';
        const roomInfo = { roomName, roomClients, roomMasterWSID };
        function JoinOrCreateRoom(_inRoomName, _inID)
        {
          if (!rooms.has(_inRoomName))
          {
              console.log("++ ROOM [" + _inRoomName + "] " + "register new room");
              rooms.set(_inRoomName, roomInfo);
          }

          if(!rooms.get(_inRoomName).roomClients.has(_inID))
          {
              console.log("++ ROOM [" + _inRoomName + "] " + "register new client wsid: " + _inID);
              rooms.get(_inRoomName).roomClients.set(_inID, clients.get(_inID));

              //assign roomMasterID
              if(rooms.get(_inRoomName).roomClients.size === 1)
              {
                SetRoomMasterWSID(_inRoomName, wsid);
              }
              else if(rooms.get(_inRoomName).roomClients.size > 1)
              {
                try { rooms.get(_inRoomName).roomClients.get(rooms.get(_inRoomName).roomMasterWSID).ws.send(FMEventEncode("OnClientConnectedEvent", wsid)); } catch {}
              }
          }

          //check room list:
          const _roomInfo_clients = rooms.get(_inRoomName).roomClients.values();
          for(var i = 0; i < rooms.get(_inRoomName).roomClients.size; i++)
          {
              console.log("** ROOM [" + _inRoomName + "] " + "(" + rooms.get(_inRoomName).roomMasterWSID + ")" + " client["+ i + "]: " + _roomInfo_clients.next().value.wsid);
          }

          console.log("== OnJoinedRoom(" + _inRoomName +")");
          ws.send(FMEventEncode("OnJoinedRoom", _inRoomName));
        }

        function IsRoomMaster(_inRoomName, _inWSID)
        {
          if (!rooms.has(_inRoomName)) return false;
          return rooms.get(_inRoomName).roomMasterWSID === _inWSID ? true : false;
        }

        function SetRoomMasterWSID(_inRoomName, _inWSID)
        {
          rooms.get(_inRoomName).roomMasterWSID = _inWSID;

          var roomClientsWSIDs ="";
          //check room list:
          var _roomInfo_clients = rooms.get(_inRoomName).roomClients.values();
          for(var i = 0; i < rooms.get(_inRoomName).roomClients.size; i++)
          {
            var roomLocalClient = _roomInfo_clients.next().value;
            roomClientsWSIDs += roomLocalClient.wsid + ",";
          }
          rooms.get(_inRoomName).roomClients.get(rooms.get(_inRoomName).roomMasterWSID).ws.send(FMEventEncode("IsRoomMasterEvent", "registered"));
          rooms.get(_inRoomName).roomClients.get(rooms.get(_inRoomName).roomMasterWSID).ws.send(FMEventEncode("OnRoomClientsUpdated", roomClientsWSIDs));
          console.log("** ROOM [" + _inRoomName + "] " + "SetRoomMasterWSID: " + _inWSID);
        }
        //

        //register it to global clients
        if(!clients.has(wsid))
        {
          ws.id = wsid;
          metadata.ws = ws;
          metadata.networkType = "";
          metadata.wsid = wsid;
          metadata.roomName = 'Lobby';
          clients.set(wsid, metadata);
          ws.send(FMEventEncode("OnReceivedWSIDEvent", wsid));
          console.log("== connection count: " + clients.size + " | wsid: " + wsid);
        }

        //ping from Server, to check connection on server side only..
        //ping from Client will be from Unity side, use string instead, to support WebGL build...
        function heartbeat() { this.isAlive = true; }
        ws.on('pong', heartbeat);
        const interval = setInterval(function ping()
        {
            if (ws.isAlive === false)
            {
              console.log("-- Terminate Timeout WSID: " + ws.wsid);
              return ws.terminate();
            }
            ws.isAlive = false;
            ws.ping();
        }, 30000);

        ws.on('close', function close()
        {
            clearInterval(interval);
            var _networkType = clients.get(wsid).networkType;
            console.log('== ON CLOSE: ' + wsid + " | " + _networkType);
            if(_networkType === 'Room')
            {
                var _roomName = clients.get(wsid).roomName;
                if(rooms.has(_roomName))
                {
                    if(rooms.get(_roomName).roomClients.has(wsid))
                    {
                        rooms.get(_roomName).roomClients.delete(wsid);
                        console.log("-- ROOM [" + _roomName + "] " + "Delete Client: " + wsid + " | client count: " + rooms.get(_roomName).roomClients.size);
                        if(rooms.get(_roomName).roomClients.size === 0)
                        {
                          rooms.delete(_roomName);
                          console.log("-- ROOM [" + _roomName + "] " + "Delete Room" + " | room count: " + rooms.size);
                        }
                        else
                        {
                            if(rooms.get(_roomName).roomMasterWSID === wsid)
                            {
                                //assign a new room master...
                                var _roomInfo_clients = rooms.get(_roomName).roomClients.values();
                                var roomLocalClient = _roomInfo_clients.next().value;
                                SetRoomMasterWSID(_roomName, roomLocalClient.wsid);
                            }
                            else
                            {
                                console.log("** Sent OnClientDisconnectedEvent to roomMasterWSID: " + rooms.get(_roomName).roomMasterWSID + " | disconnected client wsid: " + wsid);
                                try { rooms.get(_roomName).roomClients.get(rooms.get(_roomName).roomMasterWSID).ws.send(FMEventEncode("OnClientDisconnectedEvent", wsid)); } catch {}
                            }
                        }
                    }
                }
            }

            clients.delete(wsid);
            console.log("== Global Connection Count: " + clients.size);
        });

        //FMEventDecode, assuming data structure: "fmevent:type:variable"
        function FMEventEncode(inputType, inputVariable) { return "fmevent" + ":" + inputType + ":" + inputVariable; }
        function FMEventDecode(inputString) { return inputString.split(":"); }
        ws.on('message', function incoming(data, isBinary)
        {
            if (isBinary === false)
            {
                //data type: string
                var message = data.toString();
                if(message.includes("fmevent:"))
                {
                    var decodedResult = FMEventDecode(message);
                    var decodedType = decodedResult[1];
                    var decodedValue = decodedResult[2];
                    if (decodedType !== 'ping' && decodedType !== 'lping') console.log("-> Message [" + message + "]");//ignore ping message...

                    switch(decodedType)
                    {
                      case 'lping': // ws latency ping-pong test, the lping is from the queued data...
                          ws.send(FMEventEncode("lpong", decodedValue));
                          break;
                      case 'ping':
                          var myRoomName = clients.get(wsid).roomName;
                          var _isRoomMaster = IsRoomMaster(myRoomName, wsid);
                          ws.send(FMEventEncode("pong", decodedValue + "," + (_isRoomMaster ? "roomMaster" : "roomClient")));
                          break;
                      case 'networkType':
                          clients.get(wsid).networkType = decodedValue;
                          ws.send(FMEventEncode("OnJoinedLobbyEvent", clients.get(wsid).roomName));
                          console.log("regRoom(waiting request): " + wsid + " networkType: " + clients.get(wsid).networkType + " | roomName: " + clients.get(wsid).roomName);
                          break;
                      case 'roomName':
                          clients.get(wsid).roomName = decodedValue;
                          JoinOrCreateRoom(clients.get(wsid).roomName, clients.get(wsid).wsid);
                          break;
                      case 'requestRoomMaster':
                          var myRoomName = clients.get(wsid).roomName;
                          ws.send(FMEventEncode("IsRoomMasterEvent", "requested"));
                          SetRoomMasterWSID(clients.get(wsid).roomName, wsid);
                          break;
                    }
                }
            }
            else
            {
                //data type: binary bytes
                if(data.length > 4)
                {
                    if(clients.get(wsid).networkType === 'Room')
                    {
                        if(clients.get(wsid).roomName !== 'Lobby')
                        {
                            var myRoomName = clients.get(wsid).roomName;
                            var _roomInfo_clients = rooms.get(myRoomName).roomClients.values();
                            var _roomClientMyself;
                            switch(data[1])
                            {
                                case 0: //emit type: all; //check room list:
                                    for(var i = 0; i < rooms.get(myRoomName).roomClients.size; i++)
                                    {
                                      var roomLocalClient = _roomInfo_clients.next().value;
                                      if (roomLocalClient.wsid !== wsid) { roomLocalClient.ws.send(data); }
                                      else { _roomClientMyself = roomLocalClient; }
                                    }
                                    if (_roomClientMyself) _roomClientMyself.ws.send(data);
                                    break;
                                case 1: //emit type: room Master;
                                    try { rooms.get(myRoomName).roomClients.get(rooms.get(myRoomName).roomMasterWSID).ws.send(data); } catch {}
                                    break;
                                case 2: //emit type: others; //check room list:
                                    for(var i = 0; i < rooms.get(myRoomName).roomClients.size; i++)
                                    {
                                      var roomLocalClient = _roomInfo_clients.next().value;
                                      if (roomLocalClient.wsid !== wsid) roomLocalClient.ws.send(data);
                                      // console.log("room client["+ i + "]: " + _roomInfo_clients.next().value.wsid);
                                    }
                                    break;
                                case 3: //send to target
                                    var _wsidByteLength = ByteToInt16(data, 4);
                                    var _wsidByte = data.slice(6, 6 + _wsidByteLength);
                                    var _wsid = String.fromCharCode(..._wsidByte);
                                    try { if (clients.get(_wsid).roomName === myRoomName) clients.get(_wsid).ws.send(data); } catch{}
                                    break;
                            }
                        }
                    }
                }
            }
        });
    });
}

module.exports = {
  clients,
  rooms,
  uuidv4,
  ByteToInt32,
  ByteToInt16,
  initializeWebSocketHandling
};
