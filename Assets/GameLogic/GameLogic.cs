using System;
using System.Threading;

namespace WebGLMultiThreaded
{
    // as a contrived example, the game logic provides data back to Unity through two designs, and one need only pick one.
    // 1. returning state
    // 2. triggering an event
    // for events, there are a million ways to design it, from callbacks event handlers/multicast delegates, as done here.
    //
    // since all of this gets built for a non-Unity project, nothing in this folder should have no dependencies on Unity.
    public class GameLogic
    {
        private const float TimePerTick = 1;
        private float nextTime = 0;
        // NOTE: be sure to pick reasonable defaults
        private State state = new();

        // not using conventional EventHandler since we can't serialize the concept of a sender anyway, nor need it
        //
        // a past version went with multiple events/delegates per piece of state,
        // but, due to amount of plumbing needed in other places, it didn't really scale in terms of maintainability.
        public event Action<StateChange> StateChanged;


        // NOTE: thread safety is not a concern because we (currently) queue up calls to Update on a separate thread.
        // if we instead do them in parallel or just want to be defensive, Interlocked.CompareExchange is a simple way
        // to ensure only one invocation of this runs at a time.
        // an Interlocked.CompareExchange example is available in the "default" implementation, since it needs it.
        public void Update(float time)
        {
            if (time < nextTime) return;
            nextTime = time + TimePerTick;

            State old = state.Clone();
            bool changed = false;

            //some expensive operation
            Thread.Sleep(500);

            state.Counter += 3;
            state.Message = $"It is currently {DateTimeOffset.UtcNow}.";
            changed = true;
            
            // always true in this case, but here for demonstration purposes
            if (changed) StateChanged?.Invoke(new StateChange() { Old = old, New = state });
        }
    }

    [Serializable]
    public class State
    {
        // fields for JsonUtility reasons. also consider System.Text.Json or Newtonsoft.Json.
        public int Counter;
        public string Message;

        public State Clone() => new() { Counter = Counter, Message = Message };
    }
    
    [Serializable]
    public class StateChange
    {
        // fields for JsonUtility reasons. also consider System.Text.Json or Newtonsoft.Json.
        public State Old;
        public State New;
        public bool HasChanged<T>(Func<State, T> selector, out (T oldValue, T newValue) values)
        {
            values = (selector(Old), selector(New));

            // we do this check (for reference types) as a special case of the below check
            if (values.oldValue == null && values.newValue == null) return true;
            if (values.oldValue?.Equals(values.newValue) != true) return false;

            return true;
        }
    }
}