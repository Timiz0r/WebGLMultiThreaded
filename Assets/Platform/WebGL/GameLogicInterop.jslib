mergeInto(LibraryManager.library, {
  // a previous attempt tried to initialize the worker before calling `mergeInto`, but `window` doesn't exist at that point
  // instead, we need to delay initialization

  // NOTE: unity seems to drop functions that are defined with arrow functions, hence normal `function(){}`
  WebGLGameLogic_Initialize: function (eventHandler) {
    if (window.WebGLGameLogic) return;

    window.WebGLGameLogic = new class {
      constructor () {
        const worker = new Worker('interop/wwwroot/gameLogicInteropWorker.js', { type: "module" });
        worker.onmessage = e => {
          if (e.data.command === "error") {
            console.error(e.data.error);
            return;
          }

          if (e.data.command !== "stateChanged") return;

          this.handleEvent(e.data.data);
        };

        this.worker = worker;
        this.eventHandler = eventHandler;
      }

      // helpers. turns out we need to define them here too
      sendRequest(request) {
        this.worker.postMessage(request);
      }
      handleEvent(data) {
        const len = lengthBytesUTF8(data) + 1;
        const buffer = _malloc(len);
        stringToUTF8(data, buffer, len);

        const eventHandler = this.eventHandler;
        {{{ makeDynCall('vi', 'eventHandler') }}} (buffer);
        // NOTE: it's not clear if there's a risk of use-after-free here, if the callback stores the data (closure, etc.)
        // probably not a problem *if* Unity converts it to UTF16 (and doesn't otherwise use the UTF8 version).
        // but, somewhere in the .NET->il2cpp->wasm pipeline, perhaps UTF16 no longer gets used.
        //
        // also, from the docs: If the string is a return value, then the IL2CPP runtime automatically frees up the memory for you.
        // so we need to do it manually
        _free(buffer);
      }
    }();
  },

  WebGLGameLogic_Update: function (time) {
    window.WebGLGameLogic.sendRequest({ command: "update", time });
  },
});

