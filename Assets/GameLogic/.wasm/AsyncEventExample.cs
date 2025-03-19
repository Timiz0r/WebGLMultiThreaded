using System;
using System.Runtime.InteropServices.JavaScript;
using WebGLMultiThreaded;

internal partial class AsyncEventExample
{
    private static readonly GameLogic Instance = new GameLogic();

    // we need to output json because current wasm source generation doesn't support arbitrary objects
    // see issue for adding a way to easily marshall objects: https://github.com/dotnet/runtime/issues/77784
    [JSExport]
    public static void Update(float time, [JSMarshalAs<JSType.Function<JSType.String>>] Action<string> stateUpdated)
        => Instance.Update(time, state => stateUpdated(StateSerialization.Serialize(state)));

}