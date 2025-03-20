using System;
using System.Runtime.InteropServices.JavaScript;
using WebGLMultiThreaded;

internal partial class AsyncEventExample
{
    private static readonly GameLogic Instance = new GameLogic();
    static AsyncEventExample()
    {
        Instance.StateChanged += eventData => StateChanged(StateSerialization.SerializeChange(eventData));
    }

    [JSExport]
    public static void Update(float time)
        => Instance.Update(time);

    [JSImport("StateChanged", "AsyncEventExample")]
    static partial void StateChanged(string stateChangeJson);
}