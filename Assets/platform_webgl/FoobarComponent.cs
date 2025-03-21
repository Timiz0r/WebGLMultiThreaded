using System;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using WebGLMultiThreaded;

public class FoobarComponent : MonoBehaviour
{
    private static readonly System.Random rng = new();

    // if an operation is used across multiple components, it's probably desireable to encapsulate the operation
    // in another (static) class. it's currently only used in this component, so we've kept it simple.
    [DllImport("__Internal")]
    private static extern void OperationRunnerInterop_Initialize();

    [DllImport("__Internal")]
    private static extern int OperationRunnerInterop_Foobar(int num, Action<int, string> success, Action<int, string> failure);

    private static OperationRequestBuilder<FoobarResult, string> foobarOperation = OperationRequestBuilder.Create(
        success: stateJson => (FoobarResult)JsonUtility.FromJson(stateJson, typeof(FoobarResult)),
        failure: error => error
    );

    void Start()
    {
        // this call ensures multiple initialization isn't possible, but 
        OperationRunnerInterop_Initialize();
    }

    async Awaitable Update()
    {
        OperationResponse<FoobarResult, string> response = await foobarOperation.Launch(
            (success, failure) => OperationRunnerInterop_Foobar(rng.Next(100), success: success, failure: failure));

        if (!response.IsSuccess)
        {
            Debug.LogError($"Failed to run operation: {response.Error}");
            return;
        }


        FoobarResult result = response.Result;

        Transform foo = transform.Find("Foo");
        if (foo != null)
        {
            foo.GetComponent<TextMeshPro>().text = result.Foo.ToString();
        }

        Transform bar = transform.Find("Bar");
        if (bar != null)
        {
            bar.GetComponent<TextMeshPro>().text = result.Bar;
        }
    }
}
