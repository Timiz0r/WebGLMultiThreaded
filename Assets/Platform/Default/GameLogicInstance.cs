using System;
using System.Threading;
using UnityEngine;
using WebGLMultiThreaded;

public static class GameLogicInstance
{
    private static SynchronizationContext mainThread;
    private static int updateInProgress = 0;
    private static readonly GameLogic gameLogic = new();

    public static event Action<StateChange> StateChanged;

    public static void Update(float time)
    {
        if (updateInProgress == 1) return;
        // aka if the original value is 1, then another Update got ahead of this invocation
        if (Interlocked.CompareExchange(ref updateInProgress, 1, 0) == 1) return;

        ThreadPool.QueueUserWorkItem(_ =>
        {
            gameLogic.Update(time);
            updateInProgress = 0;
        });
    }

    private static void StateChangedInternal(StateChange stateChange)
        => mainThread.Post(_ => StateChanged?.Invoke(stateChange), null);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Initialize()
    {
        // both could perhaps be done in static initializer. didn't try, since this will surely work anyway.
        mainThread = SynchronizationContext.Current;
        gameLogic.StateChanged += StateChangedInternal;
    }
}
