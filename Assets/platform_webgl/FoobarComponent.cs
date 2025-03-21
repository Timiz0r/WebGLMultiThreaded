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
        // incidentally, this function's implementation ensures multiple initialization isn't possible
        OperationRunnerInterop_Initialize();
    }

    async Awaitable OnMouseDown()
    {
        OperationResponse<FoobarResult, string> response = await foobarOperation.Launch(
            (success, failure) => OperationRunnerInterop_Foobar(rng.Next(100), success: success, failure: failure));

        if (!response.IsSuccess)
        {
            Debug.LogError($"Failed to run operation: {response.Error}");
            return;
        }
        FoobarResult result = response.Result;

        Transform foobar = transform.parent;

        if (foobar.Find("Foo") is Transform foo)
        {
            foo.GetComponent<TextMeshPro>().text = result.Foo.ToString();
        }

        if (foobar.Find("Bar") is Transform bar)
        {
            bar.GetComponent<TextMeshPro>().text = result.Bar;
        }
    }
}
