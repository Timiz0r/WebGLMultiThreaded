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
            try
            {
                gameLogic.Update(time);
            }
            finally
            {
                updateInProgress = 0;
            }
        });
    }

    // NOTE: this, to me, suprisingly-ish works.
    // rather, I would expect it to *not* work if there's no code that yields any time in the main thread.
    //
    // if this implementation isn't working in your case, this would be a solution easier to reason about:
    // 1. store all event data in a thread-safe way (SemaphoreSlim or immutable collections [or thread-safe collections])
    // 2. Have SceneGameLogicRunner.(Late)Update invoke a GameLogicInstance.SendEvents (in a thread-safe way)
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
