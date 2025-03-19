using System;
using System.Runtime.InteropServices.JavaScript;

public class AsyncEventExample
{
    private static readonly GameLogic Instance = new GameLogic();

    [JSExport]
    public static void GameLogicUpdate_AsyncEventExample(float time, Action<State> stateUpdated)
    {
        Instance.Update(time, stateUpdated);
    }

}