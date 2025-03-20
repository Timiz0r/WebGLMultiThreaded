using System;
using System.Runtime.InteropServices;
using AOT;
using TMPro;
using UnityEngine;
using WebGLMultiThreaded;

public class WebGLGameLogic_AsyncEvent : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void GameLogic_Initialize_AsyncEvent();

    // NOTE: it's also hypothetically possible to pump the "request id" to unity (returned here, passed to callbacks),
    // then hook them up to async-style stuff like coroutines or async-await
    [DllImport("__Internal")]
    private static extern void GameLogic_Update_AsyncEvent(float time);

    [DllImport("__Internal")]
    private static extern void GameLogic_AsyncEventListener(string gameObjectName);

    private void StateChanged(string json)
    {
        UntypedStateChange untypedStateChange = (UntypedStateChange)JsonUtility.FromJson(json, typeof(UntypedStateChange));
        switch (untypedStateChange.Target)
        {
            case "Counter":
            {
                StateChange<int> stateChange = untypedStateChange.ConvertFrom<int>();
                var obj = transform.Find("Counter");
                obj.GetComponent<TextMeshPro>().text = stateChange.NewValue.ToString();
                break;
            }

            case "Message":
            {
                StateChange<string> stateChange = untypedStateChange.ConvertFrom<string>();
                var obj = transform.Find("Message");
                obj.GetComponent<TextMeshPro>().text = stateChange.NewValue;
                break;
            }

            default:
                Debug.LogError($"Unknown state change: {json}");
                break;
        }


    }

    private void MessageChanged(string message)
    {
        var obj = transform.Find("Message");
        obj.GetComponent<TextMeshPro>().text = message;
    }

    private static void Error(string error)
    {
        Debug.LogError($"Failed to update game logic.: {error}");
    }

    void Start()
    {
        GameLogic_Initialize_AsyncEvent();
        // these are separate in case GameLogic_AsyncEventListener is expected to be re-callable
        // of course, they can be combined if this will not happen.
        GameLogic_AsyncEventListener(name);
    }

    void Update()
    {
        GameLogic_Update_AsyncEvent(Time.time);
    }
}
