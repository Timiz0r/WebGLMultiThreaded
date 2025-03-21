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
* The "async event" approach is implemented as some game logic that regularly gets updated.
  When the state of the game changes, events are triggered.

### Shared code
* `GameLogic.cs` is a simple class with an `Update` method that will trigger events when some state changes.
* `OperationRunner.cs` contains a `Foobar` operation that performs some operation and returns a result.

In practice, the WASM-side code will actually run these,
while the Unity-side code will use the types within for deserialization from JSON.

### Unity-side
#### Unity invoking Foobar operation
`FoobarComponent.cs`, when clicked, will trigger the `Foobar` operation.
It will call `OperationRunnerInterop.jslib`'s `OperationRunnerInterop_Foobar` (and do initialization beforehand),
which will send a message to a Web Worker -- to be covered later in [WASM and Web Worker-side](#wasm-and-web-worker-side).
Eventually, when the worker respond with a message containing the result, we'll process it and update some game objects.

##### Hooking up Web Worker request and response to Awaitable
When working with Web Workers, all we do is send messages and hope we get something back in return.
To make this easier, the we've created the `OperationRequestBuilder` abstraction.

This requires certain conventions be met.
1. The call that begins the request chain (`OperationRunnerInterop_Foobar` in this case)
will take `success` and `failure` callbacks, and it will return some request id --
used to later correlate the response back to the initial request.
2. `success` and `failure` accept the request id and some string value. 
   The string value is defined by the operation and can be anything: JSON, a normal string, a numeric value.

`OperationRequestBuilder` contains the implementation of `success` and `failure`.
They get passed to the caller of `Launch`, and the caller need simply pipe them through to whatever starts the request
(`OperationRunnerInterop_Foobar`).
When a request chain is started, `OperationRequestBuilder` keeps a mapping of request id to `AwaitableCompletionSource`.
When `OperationRequestBuilder` gets a response, it finds the corresponding `AwaitableCompletionSource` and completes it.
Since `OperationRequestBuilder` is initialized with callbacks to deserialize results and errors,
it uses these to provide the right result to `AwaitableCompletionSource`.

Whatever is awaiting on the `Launch` call will then be able to do with the result as they please.

#### Unity triggering GameLogic update
`WebGLGameLogic.cs`, when Unity's `Update` is called, will simply trigger the game logic update.
It will call `GameLogicInterop.jslib`'s `GameLogicInterop_Update` (and do some initialization beforehand),
which will send a message to a Web Worker -- to be covered later in [WASM and Web Worker-side](#wasm-and-web-worker-side).
Eventually, the worker will send messages corresponding to game state updates.
In turn, these gets pumped through to `WebGLGameLogic.StateChanged`, which can process the event.

### WASM and Web Worker-side

#### Web Worker receives Foobar operation request
When the `command="Foobar"` message is received (from `OperationRunnerInterop.jslib`),
we simply invoke `OperationRunnerInterop.Foobar`.

`OperationRunnerInterop` exports the `Foobar` function,
which translates the call to the real `OperationRunner.Foobar` method.
Additionally, we must serialize the `FoobarResult` -- to JSON in this case.

We take this JSON string return value and send a response message out.
This response message gets picked up Unity-side, as described [above](#unity-invoking-foobar-operation).


### Web Worker game logic update request
When the `command="update"` message is received (from `GameLogicInterop.jslib`),
we simply invoke `GameLogicInterop.Update`.

`GameLogicInterop` exports the `Update` function,
which translates the call to the real `GameLogic.Update` method.

Additionally, `GameLogicInterop` imports a `StateChanged` function that is defined in `gameLogicInteropWorker.js`.
When `GameLogic.StateChanged` triggers, `GameLogicInterop` forwards it to this JS function, performing serialization --
to JSON in this case.

The JS-side `StateChanged` function will then send out a `stateChanged` message, which gets picked up Unity-side,
as described [above](#unity-triggering-gamelogic-update).

## Implementation notes
There are pretty ample comments that cover a lot of the implementation, as well as potential alternatives.
Of course, feel free to create an issue if something is broken/unclear/etc.

## Pain points
### Debugging
As mentioned above, play mode isn't a thing, so neither is debugging (in the modern sense).

Unity-side, debugging requires lots of console logging and rebuilds that take minutes even for small projects.
This is mainly because we can't run or debug jslib stuff in play mode, and,
to be fair, this is regardless of wanting to satisfy multi-threading requirements/desires.
Finally, once all the plumbing is hooked up, plus perhaps a bit more design magic to make things easier to extend,
the pain should go away.

C#/WASM-side, there is again no real debugging, requiring console logging.
However, non-AOT rebuilds only take a handful of seconds and don't require Unity rebuilds, so iteration is pretty quick.
This is handy, since, for the game logic example, there could be a lot of churn here.

## To do
The example wouldn't really be complete without demonstrating some pattern for cross-platform support,
so that will be the next major thing to add.

## Credits and shoutouts
The .NET WASM stuff is largely based off this repo: https://github.com/ilonatommy/reactWithDotnetOnWebWorker/tree/master

Someone did something similar Web Workers, though the goal wasn't cross-platform but was instead running user-provided JS code: https://codewithajay.com/porting-my-unity-game-to-web/
The interesting part here is that they implemented their own one-size-fits-all RPC solution.
For the example code in this project, the Web Workers and jslib files need to be extended when new events/operations are added.
Of course, I've only done it that way for simplicity, but their solution is quite nice!

Regarding the above RPC solution, it's also worth mentioning that,
[once .NET WASM multi-threading capability is fully supported](#a-note-on-multi-threaded-net-wasm),
there will be no need to for that solution (if using C# is okay), nor the Web Worker part of this solution.

## Other handy references
* Type mappings from .NET to WASM: https://learn.microsoft.com/en-us/aspnet/core/client-side/dotnet-interop/?view=aspnetcore-9.0#type-mappings
* emscripten function signature stuff (the `vii` in `{{{ makeDynCall('vii', 'callback') }}} (requestId, buffer);`):
  https://emscripten.org/docs/porting/connecting_cpp_and_javascript/Interacting-with-code.html#function-signatures
