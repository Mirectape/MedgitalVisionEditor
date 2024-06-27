mergeInto(LibraryManager.library, {
    //Unity 2021_2 or before: Pointer_stringify(_src);
    //Unity 2021_2 or above: UTF8ToString(_src);
    FMCoreTools_WebGLAddScript_2021_2: function (_innerHTML, _src)
    {
      var script = UTF8ToString(_innerHTML);
      var src = UTF8ToString(_src);
      var scriptElement = document.createElement("script");
      scriptElement.innerHTML = script;
      if (src.length > 0) scriptElement.setAttribute("src", src);
      document.head.appendChild(scriptElement);
    },
});
