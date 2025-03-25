A more verbose walkthrough can be found here: https://kyouha.today/blog/programming/unity-webgl-multithreaded-part2/

There are multiple version of the code:
* [Original](https://github.com/Timiz0r/WebGLMultiThreaded/tree/original): Implementing multi-threading in WebGL with C# and JS Web Workers
* [Crossplat](https://github.com/Timiz0r/WebGLMultiThreaded/tree/crossplat): Adding a Standalone version that uses the same C# code to update the same scene.

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

The interop classes are pretty simple.

The Web Workers deal with messages -- serialized to JSON in this demonstration.
Messages received by the workers will come from Unity
(via the WebGL `OperationRunner.FoobarAsync` and `SceneGameLogicRunner` implementations)
and trigger whatever behaviors necessary via the interop classes.
Messages sent out from the workers will be received by Unity, deserialized, and exposed
(again, via the `OperationRunner.FoobarAsync` and `SceneGameLogicRunner` classes).

##### Web Worker receives Foobar operation request
When the `command="Foobar"` message is received (from Unity-side `OperationRunnerInterop.jslib`),
we simply invoke `OperationRunner.Foobar`.

`OperationRunner` exports the `Foobar` function,
which translates the call to the real `Foobar.Execute` method.
Additionally, we must serialize the `FoobarResult` -- to JSON in this case.

We take this JSON string return value and send a response message out.
This response message gets picked up Unity-side, as described [above](#unity-invoking-foobar-operation).

##### Web Worker game logic update request
When the `command="update"` message is received (from `GameLogicInterop.jslib`),
we simply invoke `GameLogicInterop.Update`.

`GameLogicInterop` exports the `Update` function,
which translates the call to the real `GameLogic.Update` method.

Additionally, `GameLogicInterop` imports a `StateChanged` function that is defined in `gameLogicInteropWorker.js`.
When `GameLogic.StateChanged` triggers, `GameLogicInterop` forwards it to this JS function, performing serialization.

The JS-side `StateChanged` function will then send out a `stateChanged` message, which gets picked up Unity-side,
as described [above](#unity-triggering-gamelogic-update).

#### WebGL OperationRunner.FoobarAsync
A component such as `FoobarOperation` will call `OperationRunnerInterop.jslib`'s `OperationRunnerInterop_Foobar`
(and do initialization beforehand), which will send a message to the Web Worker.
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
Since play mode isn't a thing, so neither is debugging (in the modern sense).
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
The end goal is `Game logic/long-running operations <-> Unity`
(the WASM interop parts are technically shims, but they're luckily very light),
but, in between these two, we have `WASM <-> Web Worker <-> jslib`.
The WASM interop part is pretty light, so it's not much of a problem.
The real complexity is in `Web Worker <-> jslib`, where all the message passing logic needs to be done.
And there's the matter of serialization.

Once again, with .NET WASM multi-threading, the Web Worker part should go away,
removing the vast majority of the complexity, leaving fairly simple WASM and jslib shims.
The serialization part would remain, though.

As implemented in the event-like demonstration, if using a single, weakly-typed event,
adding new events luckily doesn't require changing any of these shims.
If going with separate events, they all need additional changes, though.

As implemented in the call-like demonstration, adding new operations requires changes in all the other shims.

In both cases, the changes aren't complicated, but it's obnoxious and error-prone to have so many.
Combined with difficulty in testing/debugging, potential pain!

## To do
I'd like to use Roslyn to help codegen all the jslib/WASM/Web Worker stuff/Unity-side P/Invoke code.
I've used Roslyn in Unity in the past for my localization system, so it shouldn't be *overly* hard to get started.
Of course, getting it working is another matter!

## Credits and shoutouts
The .NET WASM stuff is largely based off this repo: https://github.com/ilonatommy/reactWithDotnetOnWebWorker/tree/master

Someone did something similar Web Workers, though the goal wasn't cross-platform but was instead running user-provided JS code: https://codewithajay.com/porting-my-unity-game-to-web/
The interesting part here is that they implemented their own one-size-fits-all RPC solution.
For this demonstration project, the Web Workers and jslib files need to be extended when new events/operations are added.
For their project, this necessity goes away.
Of course, I've only done it that way for simplicity, but their solution is quite nice!

Regarding the above RPC solution, it's also worth mentioning that,
[once .NET WASM multi-threading capability is fully supported](#a-note-on-multi-threaded-net-wasm),
there will be no need to for that solution (if using C# is okay), nor the Web Worker part of this solution.

## Other handy references
* Type mappings from .NET to WASM: https://learn.microsoft.com/en-us/aspnet/core/client-side/dotnet-interop/?view=aspnetcore-9.0#type-mappings
* emscripten function signature stuff (the `viii` in `{{{ makeDynCall('viii', 'callback') }}} (requestId, buffer);`):
  https://emscripten.org/docs/porting/connecting_cpp_and_javascript/Interacting-with-code.html#function-signatures
