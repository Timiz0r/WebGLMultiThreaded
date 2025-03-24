This readme mainly contains project-related notes.
See the readme at the root of the Unity project for a full rundown.

## Platform-independent C# files not located here
This is done to allow for the Unity project to build for multiple platforms. 
Note this hasn't been implementd (yet?) in this example.
The non-WebGL builds -- which have their own threading models, can use the code directly.

## Project properties
Haven't taken to time to exhaustively test all of them for this use-case.
The most important one is `RunAOTCompilation`. Disabling it allows for, on my machine, 4s incremental builds.
Enabling it results in 50s builds, but the performance is better, of course.

A sample project doing similar things:
https://github.com/ilonatommy/reactWithDotnetOnWebWorker/blob/master/dotnet/QRGenerator.csproj

Docs:
https://github.com/dotnet/runtime/blob/main/src/mono/wasm/features.md 

## Unity should not build this code
Since this code is meant to be compiled to wasm and uses a different .NET version,
we hide it from Unity by prepending a dot.

Another option is to use conditional compilation to not build the code here --
allowing the folder to be unhidden.

Hiding the folder is done so that other artifacts (such as obj dir and whatnot) also remain hidden,
but this may not be needed.

## Publish dir
In this example, the Unity project is built to `build\webgl\` (where `build` is next to `Assets`).
We arbitrarily publish this project to `$(AssetsDir)\..\build\webgl\interop`.
In any case, note that the location of these files -- particularly the js files -- influences the paths we provide
to the `Worker`s Unity-side: `new Worker('interop/wwwroot/operationRunnerInteropWorker.js', { type: "module" })`.