using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
using WebGLMultiThreaded;

public static class GameLogicInstance
{
    // while the "default" one instantiates GameLogic itself,
    // the WASM portion (initialized by `WebGLGameLogic_Initialize`) is what instantiates it here.

    public static event Action<StateChange> StateChanged;

    public static void Update(float time)
    {
        WebGLGameLogic_Update(time);
    }

    [MonoPInvokeCallback(typeof(Action<string>))]
    private static void StateChangedInternal(string json)
    {
        StateChange stateChange = (StateChange)JsonUtility.FromJson(json, typeof(StateChange));
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
