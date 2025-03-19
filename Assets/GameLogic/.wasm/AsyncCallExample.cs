using System.Runtime.InteropServices.JavaScript;

public class AsyncCallExample
{
    private static readonly GameLogic Instance = new GameLogic();

    [JSExport]
    public static State GameLogicUpdate_AsyncCallExample(float time) => Instance.Update(time);
}