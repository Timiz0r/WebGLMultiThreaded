using System;
using System.Threading;

// as a contrived example, the game logic provides data back to Unity through two designs, and one need only pick one.
// 1. returning state
// 2. triggering an event
// for events, there are a million ways to design it, from event handlers/delegates to callbacks, as done here.
public class GameLogic
{
    private readonly State state = new();

    // we dont use time here, since I couldn't think of a clever way to use it in the example
    // of course, it's something game logic might care about, so I'd image it would typically be present.
    public State Update(float time)
    {
        //some expensive operation
        Thread.Sleep(500);

        state.IncrementCounter();

        return state;
    }

    public void Update(float time, Action<State> stateUpdated)
    {
        //some expensive operation
        Thread.Sleep(500);

        state.IncrementCounter();

        stateUpdated(state);
    }
}

public class State
{
    public int Counter { get; private set; }

    public void IncrementCounter()
    {
        Counter++;
    }
}