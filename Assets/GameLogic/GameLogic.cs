using System;
using System.Globalization;
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
        private readonly State state = new();

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

            //some expensive operation
            Thread.Sleep(500);

            ChangeState(nameof(State.Counter), state.Counter, () => state.Counter += 3);
            ChangeState(nameof(State.Message), state.Message, () => state.Message = $"It is currently {DateTimeOffset.UtcNow}.");
        }

        // this can probably be made a slight bit safer with expressions to keep these parameters consistent with each other
        // though, as long as calls are naturally written, human error should be rare. also, automated testing.
        //
        // generics are used just to provide a bit more safety but arent particularly important
        private void ChangeState<T>(string target, T oldValue, Func<T> setter)
        {
            StateChange eventData = new StateChange(target, oldValue, setter());
            StateChanged?.Invoke(eventData);
        }
    }

    public class State
    {
        public int Counter { get; set; }
        public string Message { get; set; }
    }

    public class StateChange<T>
    {
        // in the case where OldValue and NewValue are boxed and aren't evaluated more than once,
        // this is probably more efficient than having separate fields.
        // though, if often evaluated more than once, perhaps separate fields perform better? 🤷
        private readonly StateChange original;
        public string Target => original.Target;
        public T OldValue => (T)original.OldValue;
        public T NewValue => (T)original.NewValue;

        public StateChange(StateChange stateChange)
        {
            original = stateChange;
        }

        public StateChange NonGeneric() => original;
    }
    
    // needed because, by choosing to trigger an event for each individual piece of state that changes,
    // there is basically no other choice than to use objects for OldValue and NewValue.
    // well, one alternative is separate events for each piece of state, but, as mentioned above,
    // the amount of plumbing doesn't scale.
    //
    // to make things a bit safer, making Target an enum might be good, since it's a closed type.
    // for instance, a switch statement without a default label would fail to build if not all targets were covered.
    public class StateChange
    {
        public string Target { get; }
        public object OldValue { get; }
        public object NewValue { get; }

        public StateChange(string target, object oldValue, object newValue)
        {
            Target = target;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public StateChange<T> As<T>() where T : IConvertible => new StateChange<T>(this);
    }

    
    // would prefer immutable properties.
    // however, for this example code, using JsonUtility to deserialize json string, so they need to be public fields.
    // not that there arent hacks.
    //
    // this is a WebGL-exclusive dependency, but it's put here because it's used both WASM side and WebGL-side.
    [Serializable] // fun fact: JsonUtility deserialized to this without this attribute
    public class UntypedStateChange
    {
        public string Target;
        public string OldValue;
        public string NewValue;

        public static UntypedStateChange From(StateChange stateChange) => new()
        {
            Target = stateChange.Target,
            OldValue = stateChange.OldValue.ToString(),
            NewValue = stateChange.NewValue.ToString(),
        };

        // meant to be called via Unity
        public StateChange<T> ConvertTo<T>() where T : IConvertible
        {
            // FUTURE: modify StateChange`1 to also work with individual values, not just an intermediate StateChange instance
            return new StateChange(Target, convert(OldValue), convert(NewValue)).As<T>();

            // String's IConvertible implementations implicitly implemented
            T convert(IConvertible value)
                => (T)value.ToType(typeof(T), CultureInfo.InvariantCulture);
        }

        // TODO-ish: won't do it for example code, but additional overloads for complex objects and converting between json
        // would be handy.
    }
}