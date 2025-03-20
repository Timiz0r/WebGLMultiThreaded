using System;
using System.Runtime.InteropServices.JavaScript;
using WebGLMultiThreaded;

internal partial class AsyncEventExample
{
    private static readonly GameLogic Instance = new GameLogic();
    static AsyncEventExample()
    {
        Instance.CounterChanged += counter => Event("CounterChanged", counter.ToString());
        Instance.MessageChanged += message => Event("MessageChanged", message);
    }

    // we need to output json because current wasm source generation doesn't support arbitrary objects
    // see issue for adding a way to easily marshall objects: https://github.com/dotnet/runtime/issues/77784
    [JSExport]
    public static void Update(float time)
        => Instance.Update(time);

    [JSImport("event", "AsyncEventExample")]
    static partial void Event(string name, string data);
}