using System;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using WebGLMultiThreaded;

// TODO: we if we can come up with a design where multiple of these can be activated without issue
public class OperationRunner : MonoBehaviour
{
    private int lastProcessedStateSequence;

    [DllImport("__Internal")]
    private static extern void OperationRunnerInterop_Initialize();

    [DllImport("__Internal")]
    private static extern int OperationRunnerInterop_Foobar(float time, Action<int, string> success, Action<int, string> failure);

    private static OperationRequestBuilder<State, string> operationRequest = OperationRequestBuilder.Create(
        success: stateJson => (State)JsonUtility.FromJson(stateJson, typeof(State)),
        failure: error => error
    );

    void Start()
    {
        lastProcessedStateSequence = -1;
        OperationRunnerInterop_Initialize();
    }

    async Awaitable Update()
    {
        // TODO: instead of both examples being for updating game logic, make this one's scenario running some long-running operation
        // then we can avoid the weird naming  gymnatics we're doing to disambiguate both
        OperationResponse<State, string> response = await operationRequest.Launch(
            (success, failure) => OperationRunnerInterop_Foobar(Time.time, success: success, failure: failure));

        if (!response.IsSuccess)
        {
            Debug.LogError($"Failed to run operation: {response.Error}");
            return;
        }


        State state = response.Result;

        // though, == might be a better check. currentState.Sequence not being monotonic would be weird, though.
        if (state.Sequence <= lastProcessedStateSequence) return;
        lastProcessedStateSequence = state.Sequence;

        Transform counter = transform.Find("Counter");
        if (counter != null)
        {
            counter.GetComponent<TextMeshPro>().text = state.Counter.ToString();
        }

        Transform message = transform.Find("Message");
        if (message != null)
        {
            message.GetComponent<TextMeshPro>().text = state.Message;
        }
    }
}
