This readme mainly contains project-related notes.
See the readme at the root of the Unity project for a full rundown.

## Game logic C# files not located here
Instead, they are located in the parent directory.
This is done to allow for the Unity project to build for multiple platforms.
The non-WebGL builds -- which have their own threading models, can use the code directly.

Since static functions must be exported to be used JS-side,
the code in this project creates singleton instances of the game logic and exports static methods that interact with it.

## Project properties
Haven't taken to time to exhaustively test all of them for this use-case.

A sample project doing similar things:
https://github.com/ilonatommy/reactWithDotnetOnWebWorker/blob/master/dotnet/QRGenerator.csproj

Docs:
https://github.com/dotnet/runtime/blob/main/src/mono/wasm/features.md 

## Unity should not build this code
Since this code is meant to be compiled to wasm and uses a different .NET version,
we hide it from Unity by prepending a dot.

Another option is to have the wasm project folder relative to the root directory
(next to Assets).

Another option is to use conditional compilation to not build the code here --
allowing the folder to be unhidden.
Hiding the folder was done so that other artifacts (such as obj dir and whatnot) also remain hidden,
but this may not be needed.