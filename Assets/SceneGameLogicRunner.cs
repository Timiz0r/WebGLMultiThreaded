using TMPro;
using UnityEngine;
using WebGLMultiThreaded;

public class SceneGameLogicRunner : MonoBehaviour
{
    // in this example, this one component is the central place handling events and manipulating the scene.
    // one alternative is adding and invoking unity events from here
    private void StateChanged(StateChange stateChange)
    {
        switch (stateChange.Target)
        {
            case "Counter":
            {
                var obj = transform.Find("Counter");
                obj.GetComponent<TextMeshPro>().text = stateChange.As<int>().NewValue.ToString();
                break;
            }

            case "Message":
            {
                var obj = transform.Find("Message");
                obj.GetComponent<TextMeshPro>().text = stateChange.As<string>().NewValue;
                break;
            }

            default:
                Debug.LogError($"Unknown state change: {stateChange.Target}");
                break;
        }
    }

    void Start()
    {
        GameLogicInstance.StateChanged += StateChanged;
    }

    void Update()
    {
        GameLogicInstance.Update(Time.time);
    }
}