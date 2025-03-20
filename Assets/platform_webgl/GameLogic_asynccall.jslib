mergeInto(LibraryManager.library, {
  // a previous attempt tried to initialize the worker before calling `mergeInto`, but `window` doesn't exist at that point
  // instead, we need to delay initialization

  // unity seems to drop functions that are defined with arrow functions, hence normal `function(){}`
  GameLogic_Initialize_AsyncCall: function () {
    if (window.gameLogic_asynccall) return;

    window.gameLogic_asynccall = new class {
      constructor() {
        const worker = new Worker('gamelogic/wwwroot/gameLogicWorker_asynccall.js', { type: "module" });
        worker.onmessage = e => {
          if (e.data.command !== "response") { return; }
        
          const requestId = e.data.requestId;
          if (requestId == null) {
            console.error("GameLogic response has no corresponding request id.");
            return;
          }
        
          const callbacks = window.gameLogic_asynccall.pendingRequests[requestId];
          delete window.gameLogic_asynccall.pendingRequests[requestId];
          if (callbacks == null) {
            console.error("GameLogic response has no corresponding request.");
          }
        
          // the corresponding unity component will then link everything back together
          // const { success, failure } = callbacks; // apparently not supported. these vars just wont exist.
          const success = callbacks.success;
          const failure = callbacks.failure;
          if (e.data.error) {
            this.sendResponse(failure, e.data.error);
          } else {
            this.sendResponse(success, e.data.result);
          }
        };

        this.worker = worker;
        this.pendingRequests = {};
        this.nextRequestId = 0;
      }

      // helpers. turns out we need to define them here too
      sendRequest(request, success, failure) {
        const requestId = this.nextRequestId++;
        this.pendingRequests[requestId] = { success, failure };
        this.worker.postMessage({ ...request, requestId });
      }
      
      sendResponse(callback, response) {
        const len = lengthBytesUTF8(response) + 1;
        const buffer = _malloc(len);
        stringToUTF8(response, buffer, len);
        {{{ makeDynCall('vi', 'callback') }}} (buffer);
        _free(buffer);
      }
    }();
  },

  // we could hypothetically use Unity's feature to call c# methods: MyGameInstance.SendMessage('MyGameObject', 'MyFunction', 'MyString');
  // however, in order to not rely on a specific game object name, we're opting for a callback.
  // still, we could accept the game object name as a parameter if desired.
  GameLogic_Update_AsyncCall: function (time, success, failure) {
    window.gameLogic_asynccall.sendRequest({ command: "update", time }, success, failure);
  },
});

