mergeInto(LibraryManager.library, {
  // a previous attempt tried to initialize the worker before calling `mergeInto`, but `window` doesn't exist at that point
  // instead, we need to delay initialization

  // NOTE: unity seems to drop functions that are defined with arrow functions, hence normal `function(){}`
  GameLogic_Initialize_AsyncEvent: function () {
    if (window.gameLogic_asyncevent) return;

    window.gameLogic_asyncevent = new class {
      constructor () {
        const worker = new Worker('gamelogic/wwwroot/gameLogicWorker_asyncevent.js', { type: "module" });
        worker.onmessage = e => {
          if (e.data.command === "error") {
            console.error(e.data.error);
            return;
          }

          if (e.data.command !== "stateChanged") return;

          this.handleEvent(e.data.data);
        };

        this.worker = worker;
        this.eventListenerGameObjectName = undefined;
      }

      // helpers. turns out we need to define them here too
      sendRequest(request) {
        this.worker.postMessage(request);
      }
      handleEvent(data) {
        if (!this.eventListenerGameObjectName) {
          console.error("GameLogic has no registered event listener.");
          return;
        }
      
        SendMessage(this.eventListenerGameObjectName, "StateChanged", data);
      }
    }();
  },

  GameLogic_Update_AsyncEvent: function (time) {
    window.gameLogic_asyncevent.sendRequest({ command: "update", time });
  },

  // unlike the "asynccall" example, we'll actually directly call c# methods
  // in the scenario where there are many events, the alternative would be for this function to take a bunch of callbacks,
  // which we would then store
  // another interesting take involving custom-rolled rpc: https://codewithajay.com/porting-my-unity-game-to-web/
  GameLogic_AsyncEventListener: function (gameObjectName) {
    window.gameLogic_asyncevent.eventListenerGameObjectName = UTF8ToString(gameObjectName);
  }
});

// no need for functions here, but left in place to allow easier comparison to "asynccall" example.
