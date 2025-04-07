A more verbose walkthrough can be found here: https://kyouha.today/blog/programming/unity-webgl-multithreaded-part2/

There are multiple version of the code:
* [Original](https://github.com/Timiz0r/WebGLMultiThreaded/tree/original):
  Implementing multi-threading in WebGL with C# and JS Web Workers  
  Covered in [this blog post](https://kyouha.today/blog/programming/unity-webgl-multithreaded).
* [Crossplat](https://github.com/Timiz0r/WebGLMultiThreaded/tree/crossplat):
  Adding a Standalone version that uses the same C# code to update the same scene.  
  Covered in [this blog post](https://kyouha.today/blog/programming/unity-webgl-multithreaded-part2).
* [Comlink](https://github.com/Timiz0r/WebGLMultiThreaded/tree/comlink):
  Simplifying JS interop with Comlink.  
  Covered in [this blog post](https://kyouha.today/blog/programming/unity-webgl-multithreaded-part3).

## Problem
JS is single-threaded, so long-running, synchronous operations on the main thread will slow down rendering.
Ideally, we'd offload them to another thread.

### Built-in options
There is only one: [experimental `Native C/C++ Multithreading`.](https://docs.unity3d.com/Manual/webgl-technical-overview.html#multithreading-support)
Managed multithreading support does not exist.

### Browser multithreading
The way to do multi-threading in the browser is via [Web Workers](https://developer.mozilla.org/en-US/docs/Web/API/Web_Workers_API).
In brief, a worker has its own separate execution context, and communication with the main thread is done via messaging (versus references).

We can certainly use Web Workers in Unity's WebGL platform, as well,
with the help of [JavaScript plugins](https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html).
This makes implementing logic that should run on a separate thread fully doable by writing it all in JavaScript.

Or we can use WebAssembly (called by a Web Worker). Of course, WebAssembly typically isn't written directly.
Instead, we use some other language and build our WASM from it.

### Cross-platform
If we then want to support games across multiple platforms -- let's say Web and Standalone,
we might end up writing the same logic multiple times -- once in JavaScript for Web and once in C# for Standalone.

But what about WASM? Can we build the Web build's WASM from C# and use the same C# for Standalone builds?
Quick answer: Yes! That's what this project is meant to demonstrate.

## .NET WASM
First a quick note: this is not Blazor. Instead, Blazor uses this functionality.

Here's [a quick rundown of the features supported](https://github.com/dotnet/runtime/blob/main/src/mono/wasm/features.md).

There are two different docs for interop. Both are handy, so you'll probably be referencing both.
* https://learn.microsoft.com/en-us/aspnet/core/client-side/dotnet-interop (doesn't show up in sidebar)
* https://learn.microsoft.com/en-us/aspnet/core/client-side/dotnet-interop/ (has extra trailing slash)

So we can turn our C# into WASM, then use that WASM in Unity!

### A note on multi-threaded .NET WASM
There is currently [an experimental multithreading feature](https://github.com/dotnet/runtime/blob/main/src/mono/wasm/features.md#multi-threading),
having been delayed a long time and scheduled for .NET 10.
Unlike with Unity, this is full C#/.NET.
For this example code, I didn't use this feature because it's not ready yet.
However, once completed, we theoretically won't need to touch Web Workers manually anymore.
We'll just call our multi-threaded C# code (via exported WASM) directly,
making the process vastly simpler than what you are about to see.

## Building and running the example
First, there is a project in `Assets/platform_webgl/.wasm` that needs to be `dotnet publish`ed.
The project builds on .NET 9, though other versions perhaps work.
Prerequisites include...
* `sudo dotnet workload install wasm-tools`
* `sudo dotnet workload install wasm-experimental`

The Web build of the Unity project should be build to `build/webgl`.
If choosing to build in a different folder, be sure to update where the `.wasm` project publishes to.

Otherwise, `Build And Run` should work just fine! Opening the `index.html` won't work, though.

Also, as-is, play mode won't work, since play mode doesn't load JavaScript plugins
(since the Web build isn't run in play mode, no matter which platform is selected).

## The implementation flow
Let's get started!

### Two design methods
This example project contains an example for both an "async caller" approach and an "async event" approach.
* The "async caller" approach is implemented as a "Foobar operation"
  that is potentially long-running and returns some result.
  `Foobar.cs` contains a `Foobar.Execute` operation that performs some operation and returns a result.
* The "async event" approach is implemented as some game logic that regularly gets updated.
  When the state of the game changes, events are triggered.
  `GameLogic.cs` is a simple class with an `Update` method that will trigger events when some state changes.

### Platform-agnostic components
The `FoobarComponent`, when clicked, will trigger the `Foobar` operation.
This is done by invoking an intermediate `OperationRunner.FoobarAsync` -- a platform-specific implementation.

`SceneGameLogicRunner`, when Unity's `Update` is called, will trigger `GameLogic.Update`.
It also receives `GameLogic.StateChanged` events.
This is all done via an intermediate `GameLogicInstance` -- a platform-specific implementation.

### Writing platform-specific code
The two *general* ways to go about it is `#if #endif` conditional compilation and assembly definitions.
I chose assembly definitions because it results in overall cleaner projects and source code.
However, platform-specific assembly definitions make other developments tasks a bit more painful.
For instance, my `.sln` file doesn't ever include by `Platform.WebGL` assembly,
so lately I've been `Build And Running`, fixing errors, and repeating, since VS (Code) won't do any analysis of the code.

### Default/Standalone platform-specific
Since we can easily do mulit-threading in Windows/Mac/Linux, the implementation here is simple.

`GameLogicInstance` simply needs to perform `GameLogic.Update` calls on another thread,
then raise the events on the main thread.
Though, the event part *may* get a slight bit more complicated in some cases (or maybe no cases) -- as noted in the code.

`OperationRunner` is even simpler.
`Awaitable` includes `Awaitable.BackgroundThreadAsync`, so getting things running on a separate thread is easy.
Furthermore, getting the return value back to the main thread is handled by the `await` of the caller.

### WebGL platform-specific
In practice, the WASM-side code will actually run `GameLogic` and `Foobar` operation.
Over in Unity, the platform-specific `OperationRunner.FoobarAsync` and `SceneGameLogicRunner`
will communicate with JS/WASM via a Web Worker, sending and receiving messages.

#### WASM and Web Worker
The `.wasm` folder contains the WASM project that hooks up the actual implementations to Web Workers.
* `GameLogic<->GameLogicInterop<->gameLogicInteropWorker.js`
* `Foobar.Execute<->OperationInterop<->operationRunnerInteropWorker.js`

The interop classes are pretty simple, so I won't go into much detail.
The main limitation is that JS imports and exports must be static.

Fundamentally, Web Workers deal with message passing.
Internally, one handles the `message` event to receive messages and calls `postMessage` to send messages.
Externally, one calls `Worker.postMessage` to send messages to the worker
and handles the `message` event to receive messages from it.

Furthermore, in practice, "manually" implementing Web Workers is an exercise in RPC,
and both the code instantiating with worker and the caller itself need to speak the same language.
For instance, the schema of a message might have a `kind` property, as well as a `data` property specific to `kind`.
Or maybe just a `payload` property serialized to and from `protobuf`.

Instead, we can use [Comlink](https://github.com/GoogleChromeLabs/comlink)!
It basically uses the `Proxy` class to proxy `get`s and `apply`s into `postMessage`s,
and turn messages into the corresponding `get`s and `apply`s in the worker itself.

`operationRunnerInteropWorker.js` is pretty simple, since we only need to `Comlink.expose` what we get from .NET.

`gameLogicInteropWorker.js` is slightly more complicated.
First, we need to expose a `StateChanged` function so that `GameLogicInterop` can `JSImport` it.
Additionally, `Comlink` is more meant for proxying calls to a Web Worker (or alike), but it's not so great with events.
Luckily, `Comlink` proxies `set`s (in addition to `get`s, `apply`s, and probably others),
so all we need to do is call a worker later on with `worker.subscriber = ...`, and we can use that value here
for passing events back to whatever instantiated the worker.

Finally, note the `postMessage("_init");`.
Because our worker script is asynchronous, the worker isn't immediately ready to start accepting messages
and will drop them.
This is especially a problem for `gameLogicInteropWorker.js`, who will be receiving a `worker.subscriber = ...` message.
By sending this special `_init` message, the code instantiating the worker
can wait for this event before performing any additional initialization.

#### WebGL OperationRunner.FoobarAsync
A component such as `FoobarOperation` will call `OperationRunnerInterop.jslib`'s `OperationRunnerInterop_Foobar`
(and do initialization beforehand), which will call the Web Worker.
Eventually, when the worker respond with a message containing the result, we'll process it and update some game objects.

##### Hooking up Web Worker request and response to Awaitable
When working with Web Workers, all we do in practice is send messages and hope we get something back in return.
Comlink *does* simplify this by having calls (and gets) be implemented as async-awaitable `Promise`s.
Unfortunately, `Unity<->JS` interop doesn't support promises, so we have to use callbacks.
Furthermore, these callbacks in C# need to be implemented as static methods,
so we can't use closures to expose a callback per request and instead need to add a concept of a request id
to correlate calls to their return values.
To make all this easier, the we've created the `OperationRequestBuilder` abstraction.

This requires certain conventions be met.
1. The call that begins the request chain (`OperationRunnerInterop_Foobar` in this case)
   will take `success` and `failure` callbacks, and it will return some request id --
   used to later correlate the response back to the initial request.
2. `success` and `failure` accept the request id and some string value. 
   The string value is defined by the operation and can be anything: JSON, a normal string, a numeric value.

`OperationRequestBuilder` contains the implementation of `success` and `failure`,
which accept an `int` request id and `string` data.
They get passed to the caller of `Launch`, and the caller need simply pass them through to whatever starts the request
(`OperationRunnerInterop_Foobar`), which returns the request id.
When a call is initiated, `OperationRequestBuilder` keeps a mapping of request id to `AwaitableCompletionSource`.
When `OperationRequestBuilder` gets a response, it finds the corresponding `AwaitableCompletionSource` and completes it.
Since `OperationRequestBuilder` is initialized with callbacks to deserialize results and errors,
it uses these to provide the right result to `AwaitableCompletionSource`.

Whatever is awaiting on the `Launch` call will then be able to do with the result as they please.

#### WebGL GameLogicInstance
`GameLogicInstance`, when Unity's `Update` is called, will simply trigger the game logic update.
It will call `GameLogicInterop.jslib`'s `WebGLGameLogic_Update` (and do some initialization beforehand),
which will send a message to the Web Worker.
Eventually, the worker will send messages corresponding to game state updates.
In turn, these gets pumped through to `GameLogicInstance.StateChanged`, then `SceneGameLogicRunner`,
which processes the event.

## Implementation notes
There are pretty ample comments that cover a lot of the implementation, as well as potential alternatives.
Of course, feel free to create an issue if something is broken/unclear/etc.

## Pain points
How painful it is to implement does largely depend on how much effort one puts into design. Still...

### Running and debugging WebGL
The only way to do so is to `Build And Run`, which takes a rather long time if any code is changed.
Rebuilds with just scene changes are quick enough, though.
Also, non-AOT WASM builds are very quick.


#### WebGL Debugging
Since WebGL play mode isn't a thing, neither is debugging (in the modern sense).
Debugging is done via console logging.

Unity-side, debugging requires lots of console logging and rebuilds that take minutes even for small projects.
This is mainly because we can't run or debug jslib stuff in play mode, and,
to be fair, this is regardless of wanting to satisfy multi-threading requirements/desires.

C#/WASM-side, there is again no real debugging, requiring console logging.
However, non-AOT rebuilds only take a handful of seconds and don't require Unity rebuilds, so iteration is pretty quick.
This is handy, since, for the game logic example, most churn should be there as compared to Unity.

Though, to be fair, if crossplat is implemented well, then play mode or standalone builds will be sufficient for debugging.
The main part where debugging in WebGL is needed is getting the initial interop stuff hooked up.

### Plumbing
The end goal is `Game logic/long-running operations <-> Unity`,
but, in between these two, we have `WASM <-> Web Worker <-> jslib`.

Once again, with .NET WASM multi-threading, the Web Worker part should go away,
removing the vast majority of the complexity, leaving fairly simple WASM and jslib shims.
The serialization part would remain, though.

The WASM interop part is pretty light, so it's not much of a problem.

`Web Worker <-> jslib` used to be complex (see [original](https://github.com/Timiz0r/WebGLMultiThreaded/tree/original)),
but Comlink has vastly simplified this.

As implemented in the event-like demonstration, additional events would require changes to:
* WASM
* Web Worker
* jslib
* C# code that interfaces with jslib

As implemented in the call-like demonstration, adding new operations requires changes to:
* WASM
* jslib
* C# code that interfaces with jslib

In both cases, the changes aren't complicated, but they are prone to copy-paste errors.
There are the difficulties in debugging/automated testing the platform, as well.

## To do
I'd like to use Roslyn to help codegen all of the plumbing mentioned above.
I've used Roslyn in Unity in the past for my localization system, so it shouldn't be *overly* hard to get started.
Of course, getting it working is another matter!

## Credits and shoutouts
The .NET WASM stuff is largely based off this repo: https://github.com/ilonatommy/reactWithDotnetOnWebWorker/tree/master

Someone did something similar Web Workers, though the goal wasn't cross-platform but was instead running user-provided JS code: https://codewithajay.com/porting-my-unity-game-to-web/
The interesting part here is that they implemented their own one-size-fits-all RPC solution.
For this demonstration project, the Web Workers and jslib files need to be extended when new events/operations are added.
For their project, this necessity goes away.
Of course, I've only done it that way for simplicity, but their solution is quite nice!

Though, in light of me discovering Comlink, I do favor the Roslyn-based codegen approach for the crossplat scenario.

## Other handy references
* Type mappings from .NET to WASM: https://learn.microsoft.com/en-us/aspnet/core/client-side/dotnet-interop/?view=aspnetcore-9.0#type-mappings
* emscripten function signature stuff (the `viii` in `{{{ makeDynCall('viii', 'callback') }}} (requestId, buffer);`):
  https://emscripten.org/docs/porting/connecting_cpp_and_javascript/Interacting-with-code.html#function-signatures
