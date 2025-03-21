using System;
using System.Globalization;
using System.Threading;

namespace WebGLMultiThreaded
{

    // as a contrived example, the game logic provides data back to Unity through two designs, and one need only pick one.
    // 1. returning state
    // 2. triggering an event
    // for events, there are a million ways to design it, from callbacks event handlers/multicast delegates, as done here.
    public class GameLogic
    {
        private const float TimePerTick = 1;
        private float nextTime = 0;
        private readonly State state = new();

        // not using conventional EventHandler since we can't serialize the concept of a sender anyway, nor need it
        // a past version went with multiple events/delegates,
        // but, due to plumbing needed in other places, it didn't really scale in terms of maintainability.
        //
        // 
        public event Action<UntypedStateChange> StateChanged;

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
        private void ChangeState<T>(string target, T oldValue, Func<T> setter) where T : IConvertible
        {
            UntypedStateChange eventData = new UntypedStateChange()
            {
                Target =  target,
                OldValue = oldValue?.ToString(CultureInfo.InvariantCulture),
                NewValue = setter().ToString(CultureInfo.InvariantCulture)
            };

            StateChanged?.Invoke(eventData);
        }
    }

    // NOTE: would prefer immutable properties.
    // however, for this example code, using JsonUtility to deserialize json string, so they need to be public fields.
    // not that there arent hacks.
    [Serializable] // fun fact: JsonUtility deserialized to this without this attribute
    public class State
    {
        public int Counter;
        public string Message;
    }

    public class StateChange<T> where T : IConvertible
    {
        public string Target { get;}
        public T OldValue { get; }
        public T NewValue { get; }

        public StateChange(string target, T oldValue, T newValue)
        {
            Target = target;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
    
    // needed for all the fun serialization layers we need to get through
    // especially JsonUtility (where other JSON libraries could otherwise work with `StateChange<T>`)
    [Serializable]
    public class UntypedStateChange
    {
        public string Target;
        public string OldValue;
        public string NewValue;

        // meant to be called via Unity
        public StateChange<T> ConvertFrom<T>() where T : IConvertible
        {
            return new StateChange<T>(Target, convert(OldValue), convert(NewValue));

            // String's IConvertible implementations implicitly implemented
            T convert(IConvertible value)
                => (T)value.ToType(typeof(T), CultureInfo.InvariantCulture);
        }

        // TODO-ish: won't do it for example code, but additional overloads for complex objects and converting between json
        // would be handy.
    }
}