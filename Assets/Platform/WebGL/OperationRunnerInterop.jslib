mergeInto(LibraryManager.library, {
  // a previous attempt tried to initialize the worker before calling `mergeInto`, but `window` doesn't exist at that point
  // instead, we need to delay initialization

  // unity seems to drop functions that are defined with arrow functions, hence normal `function(){}`
  OperationRunnerInterop_Initialize: function () {
    if (window.operationRunnerInterop) return;

    window.operationRunnerInterop = new class {
      constructor() {
        import("https://unpkg.com/comlink/dist/esm/comlink.mjs").then(Comlink => {
          // FUTURE: interop may take time to get initialized, both the field, as well as the underlying worker
          // if necessary, consider checking interop !== undefined at its callers,
          // and consider adding some indication worker-side that initialization is complete
          this.interop = Comlink.wrap(
            new Worker('interop/wwwroot/operationRunnerInteropWorker.js', { type: "module" }));
        });
        this.nextRequestId = 0;
      }

      begin(request, success, failure) {
        const requestId = this.nextRequestId++;

        request(this.interop).then(
          result => invokeCallback(success, result),
          err => invokeCallback(failure, JSON.stringify(err)));

        return requestId;

        // NOTE: since we need to serialize WASM-side, success results should always be a string.
        // of course, errors are a bit different.
        function invokeCallback(callback, response) {
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
      }
    }();
  },

  // we could hypothetically use Unity's feature to call c# methods: SendMessage('MyGameObject', 'MyFunction', 'MyString');
  // however, in order to not rely on a specific game object name, we're opting for a callback.
  // still, we could accept the game object name as a parameter if desired.
  // also note that the async event example uses SendMessage, so take a look there for an example on using SendMessage!
  OperationRunnerInterop_Foobar: function (num, success, failure) {
    return window.operationRunnerInterop.begin(interop => interop.Foobar(num), success, failure);
  },
});

