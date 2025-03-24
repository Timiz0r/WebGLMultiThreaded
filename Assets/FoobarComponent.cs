using TMPro;
using UnityEngine;
using WebGLMultiThreaded;

public class FoobarComponent : MonoBehaviour
{
    private static readonly System.Random rng = new();

    async Awaitable OnMouseDown()
    {
        FoobarResult result = await OperationRunner.FoobarAsync(rng.Next(100));

        Transform foobar = transform.parent;

        if (foobar?.Find("Foo") is Transform foo)
        {
            foo.GetComponent<TextMeshPro>().text = result.Foo.ToString();
        }

        if (foobar?.Find("Bar") is Transform bar)
        {
            bar.GetComponent<TextMeshPro>().text = result.Bar;
        }
    }
}
