using System;
using System.Runtime.InteropServices;
using UnityEngine;
using WebGLMultiThreaded;
public static class OperationRunner
{
    private static OperationRequestBuilder<FoobarResult, string> foobarOperation = OperationRequestBuilder.Create(
        success: stateJson => (FoobarResult)JsonUtility.FromJson(stateJson, typeof(FoobarResult)),
        failure: error => error
    );

    public static async Awaitable<FoobarResult> FoobarAsync(int num)
    {
        OperationResponse<FoobarResult, string> response = await foobarOperation.Launch(
            (success, failure, initializing) => OperationRunnerInterop_Foobar(
                num, success: success, failure: failure, initializing: initializing));

        if (!response.IsSuccess)
        {
            // FUTURE: better exception
            // not logging since we don't have anything better to return
            // except null, which isn't ideal.
            throw new Exception($"Failed to run operation: {response.Error}");
        }

        return response.Result;
    }

    [DllImport("__Internal")]
    private static extern void OperationRunnerInterop_Initialize();

    [DllImport("__Internal")]
    private static extern int OperationRunnerInterop_Foobar(
        int num, Action<int, string> success, Action<int, string> failure, Action<int> initializing);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Initialize()
    {
        OperationRunnerInterop_Initialize();
    }
}