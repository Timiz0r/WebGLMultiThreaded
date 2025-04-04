mergeInto(LibraryManager.library, {
  // a previous attempt tried to initialize the worker before calling `mergeInto`, but `window` doesn't exist at that point
  // instead, we need to delay initialization

  // NOTE: unity seems to drop functions that are defined with arrow functions, hence normal `function(){}`
  WebGLGameLogic_Initialize: function (eventHandler) {
    if (window.WebGLGameLogic) return;

    window.WebGLGameLogic = new class {
      constructor () {
        const worker = new Worker('interop/wwwroot/gameLogicInteropWorker.js', { type: "module" });
        worker.onmessage = m => {
          if (m.data === "_init") {
            import("https://unpkg.com/comlink/dist/esm/comlink.mjs").then(Comlink => {
              this.interop = Comlink.wrap(worker);
              // `this` isn't what we think `this` is thanks to the Comlink proxy,
              // so we wrap the handleEvent call in an arrow func to capture the right `this`
              //
              // furthermore, this must only happen after the worker is ready to receive messages,
              // hence the extra initialization logic.
              // setting `interop.subscriber` requires Comlink to be receiving messages on the worker-side.
              this.interop.subscriber = Comlink.proxy(data => this.handleEvent(data));
              this.initComplete = true;
            });
          }
        };

        this.eventHandler = eventHandler;
      }

      update(time) {
        if (!this.initComplete) return;
        // returns a promise, but, since we won't be acting on it, just let it be!
        this.interop.Update(time);
      }

      // NOTE: since we need to serialize WASM-side, data will be a string, so no need to JSON.stringify
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
    window.WebGLGameLogic.update(time);
  },
});

