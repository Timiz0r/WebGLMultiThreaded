
const worker = new Worker('gameLogicWorker_asyncevent.js', { type: "module" });

let eventListenerGameObjectName = undefined;

worker.onmessage = e => {
  handleEvent(e.data.name, e.data.data);
};

mergeInto(LibraryManager.library, {
  GameLogic_Update_AsyncEvent: (time) => {
    sendRequest({ command: "update", time });
  },

  // unlike the "asynccall" example, we'll actually directly call c# methods
  // in the scenario where there are many events, the alternative would be for this function to take a bunch of callbacks,
  // which we would then store
  GameLogic_AsyncEventListener: (gameObjectName) => {
    eventListenerGameObjectName = gameObjectName;
  }
});

// no need for functions here, but left in place to allow easier comparison to "asynccall" example.
function sendRequest(request) {
  worker.postMessage(request);
}

function handleEvent(name, data) {
  if (!eventListenerGameObjectName) {
    console.error("GameLogic has no registered event listener.");
    return;
  }

  MyGameInstance.SendMessage(eventListenerGameObjectName, name, data);
}