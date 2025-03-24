using System.Runtime.InteropServices.JavaScript;

internal partial class OperationInterop
{
    // we need to output json because current wasm source generation doesn't support arbitrary objects
    // see issue for adding a way to easily marshall objects: https://github.com/dotnet/runtime/issues/77784
    [JSExport]
    public static string Foobar(int num) => InteropSerialization.Serialize(WebGLMultiThreaded.Foobar.Execute(num));
}