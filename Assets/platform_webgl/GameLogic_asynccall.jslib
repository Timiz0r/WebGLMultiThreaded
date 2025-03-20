const worker = new Worker('gameLogicWorker_asynccall.js', { type: "module" });

const pendingRequests = {};
let nextRequestId = 0;
worker.onmessage = e => {
  if (e.data.command !== "response") { return; }

  const requestId = e.data.requestId;
  if (requestId == null) {
    console.error("GameLogic response has no corresponding request id.");
    return;
  }

  const callbacks = pendingRequests[requestId];
  delete pendingRequests[requestId];
  if (callbacks == null) {
    console.error("GameLogic response has no corresponding request.");
  }

  // the corresponding unity component will then link everything back together
  const { success, failure } = callbacks;
  if (e.data.error) {
    sendResponse(failure, e.data.error);
  } else {
    sendResponse(success, e.data.result);
  }
};

mergeInto(LibraryManager.library, {
  // we could hypothetically use Unity's feature to call c# methods: MyGameInstance.SendMessage('MyGameObject', 'MyFunction', 'MyString');
  // however, in order to not rely on a specific game object name, we're opting for a callback.
  // still, we could accept the game object name as a parameter if desired.
  GameLogic_Update_AsyncCall: (time, success, failure) => {
    sendRequest({ command: "update", time }, success, failure);
  },
});

function sendRequest(request, success, failure) {
  const requestId = nextRequestId++;
  pendingRequests[requestId] = { success, failure };
  worker.postMessage({ ...request, requestId });
}

function sendResponse(callback, response) {
  const len = lengthBytesUTF8(response) + 1;
  const buffer = _malloc(len);
  stringToUTF8(response, buffer, len);
  {{{ makeDynCall('vi', 'callback') }}} (buffer);
  _free(buffer);
}