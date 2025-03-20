using System;
using System.Runtime.InteropServices;
using AOT;
using TMPro;
using UnityEngine;
using WebGLMultiThreaded;

public class WebGLGameLogic_AsyncEvent : MonoBehaviour
{
    // NOTE: it's also hypothetically possible to pump the "request id" to unity (returned here, passed to callbacks),
    // then hook them up to async-style stuff like coroutines or async-await
    [DllImport("__Internal")]
    private static extern void GameLogic_Update_AsyncEvent(float time);

    [DllImport("__Internal")]
    private static extern void GameLogic_AsyncEventListener(string gameObjectName);

    private void CounterChanged(int counter)
    {
        var obj = transform.Find("Counter");
        obj.GetComponent<TextMeshPro>().text = counter.ToString();
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
        GameLogic_AsyncEventListener(name);
    }

    void Update()
    {
        GameLogic_Update_AsyncEvent(Time.time);
    }
}
