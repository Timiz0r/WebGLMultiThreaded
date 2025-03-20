using System;
using System.Threading;

namespace WebGLMultiThreaded
{

    // as a contrived example, the game logic provides data back to Unity through two designs, and one need only pick one.
    // 1. returning state
    // 2. triggering an event
    // for events, there are a million ways to design it, from callbacks event handlers/multicast delegates, as done here.
    public class GameLogic
    {
        private const float TimePerTick = 1000;
        private float nextTime = 0;
        private readonly State state = new();

        public event Action<int> CounterChanged;
        public event Action<string> MessageChanged;

        public State Update(float time)
        {
            // using sequence to track state changes, to reduce workload in Unity-side update.
            // however, this implementation technically does unnecessary serialization work.
            // this can be reduced with a Result { State, Changed: bool } sort of type if needed.
            // certainly other ways to track state changes (such as event example).
            // or could always not track changes if one's scenario prefers always updating.
            if (time < nextTime) return state;
            // just mentioning this because it may be unwise to emulate this kind of logic.
            nextTime = time + TimePerTick;

            //some expensive operation
            Thread.Sleep(500);

            state.Counter++;
            CounterChanged.Invoke(state.Counter);

            state.Message = $"It is currently {DateTimeOffset.UtcNow}.";
            MessageChanged.Invoke(state.Message);

            state.Sequence++;

            return state;
        }
    }

    public class State
    {
        // NOTE: would prefer immutable properties.
        // however, for this example code, using JsonUtility to deserialize json string, so they need public setters.
        public int Counter { get; set; }
        public string Message { get; set;}
        // meant to allow change tracking
        // previous `Update` versions would return null if no changes, but this made implementation obnoxious.
        // so would not recommend returning null from `Update`.
        public int Sequence;
    }
}