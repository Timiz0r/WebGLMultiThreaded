using System.Text.Json;
using System.Text.Json.Serialization;
using WebGLMultiThreaded;

// without source generation, we get this kind of warning (since post-AOT, won't be able to reflect types)
// Using member 'System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)'
// which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code.
// JSON serialization and deserialization might require types that cannot be statically analyzed.
// Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.(IL2026)
//
// so we use source gen to build a SourceGenerationContext. multiple JsonSerializable attributes can be added as needed.
[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(State))]
[JsonSerializable(typeof(UntypedStateChange))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}

internal static class StateSerialization
{
    public static string Serialize(State state)
        => JsonSerializer.Serialize(state, typeof(State), SourceGenerationContext.Default);

    public static string SerializeChange(UntypedStateChange stateChange)
        => JsonSerializer.Serialize(stateChange, typeof(UntypedStateChange), SourceGenerationContext.Default);
}