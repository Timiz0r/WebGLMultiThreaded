using System.Runtime.InteropServices.JavaScript;
using WebGLMultiThreaded;

internal partial class GameLogicInterop
{
    private static readonly GameLogic Instance = new GameLogic();
    static GameLogicInterop()
    {
        Instance.StateChanged += eventData => StateChanged(InteropSerialization.Serialize(eventData));
    }

    [JSExport]
    public static void Update(float time)
        => Instance.Update(time);

    [JSImport("StateChanged", "GameLogic")]
    static partial void StateChanged(string stateChangeJson);
}