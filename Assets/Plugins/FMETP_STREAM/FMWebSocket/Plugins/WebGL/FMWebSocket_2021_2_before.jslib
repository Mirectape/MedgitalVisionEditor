mergeInto(LibraryManager.library, {
  //Unity 2021_2 or before: Pointer_stringify(_src);
  //Unity 2021_2 or above: UTF8ToString(_src);
  FMWebSocket_IsWebSocketConnected_2021_2_before: function ()
  {
    var returnStr = "-1";
    if(websocket !== null)
    {
      returnStr = websocket.readyState.toString();
      // if(websocket.readyState === WebSocket.OPEN || websocket.readyState === WebSocket.CLOSING)
      // {
      //   returnStr = "true";
      // }
    }
    var bufferSize = lengthBytesUTF8(returnStr) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(returnStr, buffer, bufferSize);
    return buffer;
  },

  FMWebSocket_AddEventListeners_2021_2_before: function (_src, _gameobject)
  {
    var src = Pointer_stringify(_src);
    var gameobject = Pointer_stringify(_gameobject);

    websocket = new WebSocket(src);
    websocket.binaryType = 'arraybuffer';
    websocket.onopen = function(evt) { onOpen(evt) };
    websocket.onclose = function(evt) { onClose(evt) };
    websocket.onmessage = function(evt) { onMessage(evt) };
    websocket.onerror = function(evt) { onError(evt) };

    //fixed bug: unityNamespace is not available during the dynamic page loading when the game is not fully loaded
    //suggested by Jom000: https://forum.unity.com/threads/670270/page-25#post-9872856
    if (typeof window !== 'undefined' && typeof window.unityInstance === 'undefined')
    {
      console.error('No unityInstance in page');
      if (window.unityNamespace && window.unityNamespace.Module)
      {
        console.error('try to definde unityInstance');
        window.unityInstance = window.unityNamespace.Module;
      } else { console.error('unityNamespace not ready'); }
    }
    //fixed bug: unityNamespace is not available during the dynamic page loading when the game is not fully loaded
    
    var foundInstance = false;
    var webInstance;
    try { webInstance = gameInstance; foundInstance = true;} catch {}
    if ( foundInstance === false)
    {
      try { webInstance = unityInstance; foundInstance = true;} catch {}
    }
    function UnitySendMessage(_gameobject, _message) { webInstance.SendMessage(_gameobject, _message); }
    function UnitySendMessage(_gameobject, _message, _data) { webInstance.SendMessage(_gameobject, _message, _data); }

    function onOpen(evt)
    {
      if(websocket === null) return;
      if(websocket.readyState === WebSocket.OPEN)
      {
        UnitySendMessage(gameobject, 'RegOnOpen');
      }
    }

    function onClose(evt)
    {
      if(websocket === null) return;

      UnitySendMessage(gameobject, 'RegOnClose');
      websocket = null;
    }

    function onError(evt)
    {
      if(websocket === null) return;
      UnitySendMessage(gameobject, 'RegOnError', evt.data);
    }

    function onMessage(evt)
    {
      if(websocket === null) return;
      if(typeof evt.data === "string")
      {
        // if(evt.data!=="heartbeat") console.log("this is string data!");
        UnitySendMessage(gameobject, 'RegOnMessageString', evt.data);
      }
      else if(evt.data instanceof ArrayBuffer)
      {
        var _byteRaw = new Uint8Array(evt.data);

        // console.log("this is array buffer!");
        var bytes = new Uint8Array(_byteRaw);
        //----conver byte[] to Base64 string----
        var len = bytes.byteLength;
        var binary = '';
        for (var i = 0; i < len; i++) binary += String.fromCharCode(bytes[i]);
        var base64String = btoa(binary);
        //----conver byte[] to Base64 string----

        UnitySendMessage(gameobject, 'RegOnMessageRawData', base64String);
      }
    }

    function CombineInt8Array(a, b, offset)
    {
        var c = new Int8Array(a.length);
        c.set(a);
        c.set(b, offset);
        return c;
    }
    function ByteToInt32(_byte, _offset)
    {
        return (_byte[_offset] & 255) + ((_byte[_offset + 1] & 255) << 8) + ((_byte[_offset + 2] & 255) << 16) + ((_byte[_offset + 3] & 255) << 24);
    }
    function ByteToInt16(_byte, _offset)
    {
        return (_byte[_offset] & 255) + ((_byte[_offset + 1] & 255) << 8);
    }
    // end Audio functions
  },

  FMWebSocket_SendByte_2021_2_before: function(_array, _size)
  {
    const newArray = new ArrayBuffer(_size);
    const newByteArray = new Uint8Array(newArray);
    for(var i = 0; i < _size; i++) newByteArray[i] = HEAPU8[_array + i];
    try { websocket.send(newByteArray); } catch { }
  },

  FMWebSocket_SendString_2021_2_before: function (_src)
  {
    var stringData = Pointer_stringify(_src);
    try { websocket.send(stringData); } catch { }
  },

  FMWebSocket_Close_2021_2_before: function (_e, _data)
  {
    if(websocket === null) return;
    if(websocket.readyState === WebSocket.OPEN) websocket.close();
  },
});
