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

    // NOTE: it's also hypothetically possible to pump the "request id" to unity (returned here, passed to callbacks),
    // then hook them up to async-style stuff like coroutines or async-await
    [DllImport("__Internal")]
    private static extern void GameLogic_Update_AsyncCall(float time, Action<string> success, Action<string> failure);

    [DllImport("__Internal")]
    private static extern void GameLogic_Initialize_AsyncCall();
    [MonoPInvokeCallback(typeof(Action<string>))]
    private static void UpdateSuccess(string stateJson)
    {
        currentState = (State)JsonUtility.FromJson(stateJson, typeof(State));
    }

    // TODO: see if we can provide this thru lambdas. if so, we'll leave UpdateSuccess in order to show both examples.
    [MonoPInvokeCallback(typeof(Action<string>))]
    private static void UpdateFailure(string error)
    {
        Debug.LogError($"Failed to update game logic.: {error}");
    }

    void Start()
    {
        currentState = new() { Message = "State not yet updated." };
        GameLogic_Initialize_AsyncCall();
    }

    void Update()
    {
        GameLogic_Update_AsyncCall(Time.time, UpdateSuccess, UpdateFailure);

        var counter = transform.Find("Counter");
        counter.GetComponent<TextMeshPro>().text = currentState.Counter.ToString();

        var message = transform.Find("Message");
        message.GetComponent<TextMeshPro>().text = currentState.Message;
    }
}
