mergeInto(LibraryManager.library, {
  // a previous attempt tried to initialize the worker before calling `mergeInto`, but `window` doesn't exist at that point
  // instead, we need to delay initialization

  // NOTE: unity seems to drop functions that are defined with arrow functions, hence normal `function(){}`
  WebGLGameLogic_Initialize: function () {
    if (window.WebGLGameLogic) return;

    window.WebGLGameLogic = new class {
      constructor (eventHandler) {
        const worker = new Worker('interop/wwwroot/WebGLGameLogicWorker.js', { type: "module" });
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
        const eventHandler = this.eventHandler;
        {{{ makeDynCall('vi', 'eventHandler') }}} (data);
      }
    }();
  },

  WebGLGameLogic_Update: function (time) {
    window.WebGLGameLogic.sendRequest({ command: "update", time });
  },
});

