using System.Text.Json.Serialization;

namespace Lilly.Engine.Lua.Scripting.Data;

/// <summary>
/// Completion configuration for Lua Language Server
/// </summary>
public class LuarcCompletionConfig
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; } = true;

    [JsonPropertyName("callSnippet")]
    public string CallSnippet { get; set; } = "Replace";
}
