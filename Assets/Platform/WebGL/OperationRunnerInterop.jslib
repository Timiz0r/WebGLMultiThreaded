mergeInto(LibraryManager.library, {
  // a previous attempt tried to initialize the worker before calling `mergeInto`, but `window` doesn't exist at that point
  // instead, we need to delay initialization

  // unity seems to drop functions that are defined with arrow functions, hence normal `function(){}`
  OperationRunnerInterop_Initialize: function () {
    if (window.operationRunnerInterop) return;

    window.operationRunnerInterop = new class {
      constructor() {
        const worker = new Worker('interop/wwwroot/operationRunnerInteropWorker.js', { type: "module" });
        worker.onmessage = e => {
          const command = e.data.command;
          const requestId = e.data.requestId;
        
          // in some sense, there's no need to track this here, since we link them together unity-side
          // however, we need to get the right callbacks called, meaning we need to track request ids here, as well.
          // a hypothetical alternative is to pass them via OperationRunnerInterop_Initialize, but this current way feels safer.
          const callbacks = window.operationRunnerInterop.pendingRequests[requestId];
          delete window.operationRunnerInterop.pendingRequests[requestId];
          if (callbacks == null) {
            console.error("Operation response has no corresponding request.");
            return;
          }
          // the corresponding unity component will then link everything back together
          // const { success, failure, initializing } = callbacks; // apparently not supported. these vars just wont exist.
          const success = callbacks.success;
          const failure = callbacks.failure;
          const initializing = callbacks.initializing;

          // aka the worker isn't ready to handle requests. instead of keeping them queued, we throw them out.
          if (command === "initializing") {
            this.sendResponse(initializing, requestId, null);
            return;
          }

          if (command === "error") {
            this.sendResponse(failure, requestId, e.data.error);
            return;
          }

          if (command === "response") {
            this.sendResponse(success, requestId, e.data.result);
            return;
          }

          console.error("Unknown command: ", command);
        };

        this.worker = worker;
        this.pendingRequests = {};
        this.nextRequestId = 0;
      }

      // helpers. turns out we need to define them here too
      sendRequest(request, success, failure, initializing) {
        const requestId = this.nextRequestId++;
        this.pendingRequests[requestId] = { success, failure, initializing };
        this.worker.postMessage({ ...request, requestId });
        return requestId;
      }
      
      sendResponse(callback, requestId, response) {
        const len = lengthBytesUTF8(response) + 1;
        const buffer = _malloc(len);
        stringToUTF8(response, buffer, len);
        {{{ makeDynCall('vii', 'callback') }}} (requestId, buffer);
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

  // we could hypothetically use Unity's feature to call c# methods: SendMessage('MyGameObject', 'MyFunction', 'MyString');
  // however, in order to not rely on a specific game object name, we're opting for a callback.
  // still, we could accept the game object name as a parameter if desired.
  // also note that the async event example uses SendMessage, so take a look there for an example on using SendMessage!
  OperationRunnerInterop_Foobar: function (num, success, failure, initializing) {
    return window.operationRunnerInterop.sendRequest({ command: "Foobar", num }, success, failure, initializing);
  },
});

