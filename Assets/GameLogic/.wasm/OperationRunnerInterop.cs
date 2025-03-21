using System.Runtime.InteropServices.JavaScript;
using WebGLMultiThreaded;

internal partial class OperationRunnerInterop
{
    private static readonly GameLogic Instance = new GameLogic();

    // we need to output json because current wasm source generation doesn't support arbitrary objects
    // see issue for adding a way to easily marshall objects: https://github.com/dotnet/runtime/issues/77784
    [JSExport]
    public static string Foobar(float time) => StateSerialization.Serialize(Instance.Update(time));
}