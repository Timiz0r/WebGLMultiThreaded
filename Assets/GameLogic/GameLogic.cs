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
            // could also pump through if the state is actually updated. depends on use case, so not worrying about it in this example code.
            if (time < nextTime) return state;
            // NOTE: still mulling over the best implementation for this kind of logic, but not important for this example
            // (in particular mulling over what to do if processing a tick takes more than TimePerTick).
            // just mentioning this because it may be unwise to emulate this kind of logic.
            nextTime = time + TimePerTick;

            //some expensive operation
            Thread.Sleep(500);

            state.Counter++;
            CounterChanged.Invoke(state.Counter);

            state.Message = $"It is currently {DateTimeOffset.UtcNow}.";
            MessageChanged.Invoke(state.Message);

            return state;
        }
    }

    public class State
    {
        // NOTE: would prefer immutable properties.
        // however, for this example code, using JsonUtility to deserialize json string, so they need public setters.
        public int Counter { get; set; }
        public string Message { get; set;}
    }
}