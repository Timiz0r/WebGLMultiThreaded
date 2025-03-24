using UnityEngine;
using WebGLMultiThreaded;
public static class OperationRunner
{
    public static async Awaitable<FoobarResult> FoobarAsync(int num)
    {
        await Awaitable.BackgroundThreadAsync();
        FoobarResult result = Foobar.Execute(num);
        // if caller is main thread, will await over there will get us back to main thread
        return result;
    }
}