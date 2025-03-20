using System;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using WebGLMultiThreaded;


public class WebGLGameLogic_AsyncCall : MonoBehaviour
{
    private int lastProcessedStateSequence;

    [DllImport("__Internal")]
    private static extern int GameLogic_Update_AsyncCall(float time, Action<int, string> success, Action<int, string> failure);

    [DllImport("__Internal")]
    private static extern void GameLogic_Initialize_AsyncCall();

    private static AwaitableRequestBuilder<State, string> gameLogicUpdater = AwaitableRequestBuilder.Create(
        success: stateJson => (State)JsonUtility.FromJson(stateJson, typeof(State)),
        failure: error => error
    );

    void Start()
    {
        lastProcessedStateSequence = -1;
        GameLogic_Initialize_AsyncCall();
    }

    async Awaitable Update()
    {
        // TODO: instead of both examples being for updating game logic, make this one's scenario running some long-running operation
        AwaitableRequestResponse<State, string> response = await gameLogicUpdater.Launch(
            (success, failure) => GameLogic_Update_AsyncCall(Time.time, success: success, failure: failure));

        if (!response.IsSuccess)
        {
            Debug.LogError($"Failed to update game logic.: {response.Error}");
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
