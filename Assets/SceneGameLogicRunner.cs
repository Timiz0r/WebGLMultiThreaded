using TMPro;
using UnityEngine;
using WebGLMultiThreaded;

public class SceneGameLogicRunner : MonoBehaviour
{
    // in this example, this one component is the central place handling events and manipulating the scene.
    // one alternative is adding and invoking unity events from here
    private void StateChanged(StateChange stateChange)
    {
        transform.Find("Counter").GetComponent<TextMeshPro>().text = stateChange.New.Counter.ToString();
        transform.Find("Message").GetComponent<TextMeshPro>().text = stateChange.New.Message;
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