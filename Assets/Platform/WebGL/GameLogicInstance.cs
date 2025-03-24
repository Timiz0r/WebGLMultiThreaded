using System;
using System.Runtime.InteropServices;
using AOT;
using TMPro;
using UnityEngine;
using WebGLMultiThreaded;

public static class GameLogicInstance
{
    // while the "default" one instantiates GameLogic itself,
    // the WASM portion (initialized by `WebGLGameLogic_Initialize`) is what instantiates it here.

    public static event Action<StateChange> StateChanged;

    public static void Update()
    {
        WebGLGameLogic_Update(Time.time);
    }

    // FUTURE/NOTE: pain point!
    // Standalone platform will keep its StateChange type without a problem.
    // we want to replicate this behavior WebGL-side, and, as currently designed, this requires doing the conversion over here.
    // any time we add a new event, we must modify this method. potential solutions:
    // * codegen
    // * use some general RPC solution so that type information isn't lost when we serialize WebGL stuff
    // * have GameLogic.StateChanged use UntypedStateChange, to match what WebGL has to do
    [MonoPInvokeCallback(typeof(Action<string>))]
    private static void StateChangedInternal(string json)
    {
        UntypedStateChange untypedStateChange = (UntypedStateChange)JsonUtility.FromJson(json, typeof(UntypedStateChange));
        StateChange stateChange = untypedStateChange.Target switch {
            "Counter" => untypedStateChange.ConvertTo<int>().NonGeneric(),
            "Message" => untypedStateChange.ConvertTo<string>().NonGeneric(),
            _ => null
        };
        if (stateChange is null)
        {
            Debug.LogError($"Unknown state change: {json}");
            return;
        }
        StateChanged?.Invoke(stateChange);
    }


    [DllImport("__Internal")]
    private static extern void WebGLGameLogic_Initialize(Action<string> eventHandler);

    [DllImport("__Internal")]
    private static extern void WebGLGameLogic_Update(float time);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Initialize()
    {
        WebGLGameLogic_Initialize(StateChangedInternal);
    }
}
