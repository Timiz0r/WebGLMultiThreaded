using System.Runtime.InteropServices.JavaScript;
using WebGLMultiThreaded;

internal partial class GameLogicInterop
{
    private static readonly GameLogic Instance = new GameLogic();
    static GameLogicInterop()
    {
        // we need to output json because current wasm source generation doesn't support arbitrary objects
        // see issue for adding a way to easily marshall objects: https://github.com/dotnet/runtime/issues/77784
        Instance.StateChanged += stateChange => StateChanged(InteropSerialization.Serialize(stateChange));
    }

    [JSExport]
    public static void Update(float time)
        => Instance.Update(time);

    [JSImport("StateChanged", "GameLogic")]
    static partial void StateChanged(string stateChangeJson);
}