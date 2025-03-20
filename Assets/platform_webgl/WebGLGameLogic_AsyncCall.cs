using System;
using System.Runtime.InteropServices;
using AOT;
using TMPro;
using UnityEngine;
using WebGLMultiThreaded;

public class WebGLGameLogic_AsyncCall : MonoBehaviour
{
    // NOTE: it's important to be aware that this is static -- required for pinvoke reasons
    // at least it's encapsulated to just this component, but things could go weird if multiple of these components were active.
    private static State currentState;
    private int lastProcessedStateSequence;

    [DllImport("__Internal")]
    private static extern void GameLogic_Update_AsyncCall(float time, Action<string> success, Action<string> failure);

    [DllImport("__Internal")]
    private static extern void GameLogic_Initialize_AsyncCall();


    // NOTE: it's also hypothetically possible to pump the "request id" to unity (returned here, passed to callbacks),
    // then hook them up to async-style stuff like coroutines or async-await.
    // instead, for simplicity, we'll simply update off the callback.
    //
    // while this seems like an "event-driven approach", I'm calling it a "call-based approach"
    // based on the perspective of GameLogic's implementation.
    // again, plumbing async-await or coroutines are a way to get fully "call-like semantics" Unity-side!
    [MonoPInvokeCallback(typeof(Action<string>))]
    private static void UpdateSuccess(string stateJson)
    {
        // would be somewhat ideal to update from here, but we cant because this callback needs to be static
        currentState = (State)JsonUtility.FromJson(stateJson, typeof(State));
    }

    // quick note: c# 10 gets lambda attributes! unity on c# 9 though.
    [MonoPInvokeCallback(typeof(Action<string>))]
    private static void UpdateFailure(string error)
    {
        Debug.LogError($"Failed to update game logic.: {error}");
    }

    void Start()
    {
        lastProcessedStateSequence = -1;
        GameLogic_Initialize_AsyncCall();
    }

    void Update()
    {
        GameLogic_Update_AsyncCall(
            Time.time,
            [MonoPInvokeCallback(typeof(Action<string>))] (stateJson) => currentState = (State)JsonUtility.FromJson(stateJson, typeof(State)),
            UpdateFailure);

        // first update from game logic hasnt finished yet
        if (currentState == null) return;

        // though, == might be a better check. currentState.Sequence not being monotonic would be weird, though.
        if (currentState.Sequence <= lastProcessedStateSequence) return;
        lastProcessedStateSequence = currentState.Sequence;

        Transform counter = transform.Find("Counter");
        if (counter != null)
        {
            counter.GetComponent<TextMeshPro>().text = currentState.Counter.ToString();
        }

        Transform message = transform.Find("Message");
        if (message != null)
        {
            message.GetComponent<TextMeshPro>().text = currentState.Message;
        }
    }
}
