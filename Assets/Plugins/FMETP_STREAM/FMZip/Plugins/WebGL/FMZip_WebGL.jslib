mergeInto(LibraryManager.library, {
  //Unity 2021_2 or before: Pointer_stringify(_src);
  //Unity 2021_2 or above: UTF8ToString(_src);
    FMUnzipBytesJSAsync: function(_array, _size, _callbackID, _callback)
    {
      const newArray = new ArrayBuffer(_size);
      const newByteArray = new Uint8Array(newArray);
      for(var i = 0; i < _size; i++) newByteArray[i] = HEAPU8[_array + i];

      var gunzip = new Zlib.Gunzip (newByteArray);
      var bytes = gunzip.decompress();

      var outputLength = bytes.length;
      var outputPtr = _malloc(outputLength);
      Module.HEAPU8.set(bytes, outputPtr);
      Module.dynCall_viii(_callback, _callbackID, outputLength, outputPtr);
    },
    //
    // FMUnzipBytes_2021_2: function(_array, _size)
    // {
    //   // var gameobject = UTF8ToString(_gameobject);
    //
    //   const newArray = new ArrayBuffer(_size);
    //   const newByteArray = new Uint8Array(newArray);
    //   for(var i = 0; i < _size; i++) newByteArray[i] = HEAPU8[_array + i];
    //
    //   var gunzip = new Zlib.Gunzip (newByteArray);
    //   var bytes = gunzip.decompress();
    //
    //   //----conver byte[] to Base64 string----
    //   var len = bytes.byteLength;
    //   var binary = '';
    //   for (var i = 0; i < len; i++) binary += String.fromCharCode(bytes[i]);
    //   var base64String = btoa(binary);
    //   //----conver byte[] to Base64 string----
    //   // try { gameInstance.SendMessage(gameobject, 'OnFMUnzipBytes', base64String); } catch(e) {}
    //   // try { unityInstance.SendMessage(gameobject, 'OnFMUnzipBytes', base64String); } catch(e) {}
    //
    //   var base64String_length = lengthBytesUTF8(base64String) + 1;
    //   var strPtr = _malloc(base64String_length);
    //   stringToUTF8(base64String, strPtr, base64String_length);
    //
    //   return strPtr;
    // },
});
