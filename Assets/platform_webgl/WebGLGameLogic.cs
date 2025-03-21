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

    [DllImport("__Internal")]
    private static extern void GameLogicInterop_Update(float time);

    [DllImport("__Internal")]
    private static extern void GameLogicInterop_RegisterEventListener(string gameObjectName);

    // in this example, this one component is the central place handling events and manipulating the scene.
    // one alternative is adding and invoking unity events from here
    // another alternative is having "intermediate" .net events
    //   for instance, first add `public static event`s here.
    //   then, other components can listen to them as they wish.
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
