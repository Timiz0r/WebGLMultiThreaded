using System;
using System.Runtime.InteropServices;
using AOT;
using TMPro;
using UnityEngine;
using WebGLMultiThreaded;

public class WebGLGameLogic : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void GameLogicInterop_Initialize();

    // NOTE: it's also hypothetically possible to pump the "request id" to unity (returned here, passed to callbacks),
    // then hook them up to async-style stuff like coroutines or async-await
    [DllImport("__Internal")]
    private static extern void GameLogicInterop_Update(float time);

    [DllImport("__Internal")]
    private static extern void GameLogicInterop_RegisterEventListener(string gameObjectName);

    // TODO: instead of driving everything from one component, allow, for instance, counter to get state itself
    // this component simply does the initial legwork
    // this work will also include rearranging the scene objects more sanely. 
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
        GameLogicInterop_Initialize();
        // these are separate in case GameLogicInterop_RegisterEventListener is expected to be re-callable
        // of course, they can be combined if this will not happen.
        GameLogicInterop_RegisterEventListener(name);
    }

    void Update()
    {
        GameLogicInterop_Update(Time.time);
    }
}
