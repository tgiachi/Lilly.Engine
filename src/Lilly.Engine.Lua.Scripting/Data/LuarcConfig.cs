using System.Text.Json.Serialization;

namespace Lilly.Engine.Lua.Scripting.Data;

/// <summary>
/// Configuration class for Lua Language Server (.luarc.json file)
/// </summary>
public class LuarcConfig
{
    [JsonPropertyName("$schema")]

    /// <summary>
    /// 
    /// </summary>
    public string Schema { get; set; } = "https://raw.githubusercontent.com/sumneko/vscode-lua/master/setting/schema.json";

    [JsonPropertyName("runtime")]
    public LuarcRuntimeConfig Runtime { get; set; } = new();

    [JsonPropertyName("workspace")]
    public LuarcWorkspaceConfig Workspace { get; set; } = new();

    [JsonPropertyName("diagnostics")]
    public LuarcDiagnosticsConfig Diagnostics { get; set; } = new();

    [JsonPropertyName("completion")]
    public LuarcCompletionConfig Completion { get; set; } = new();

    [JsonPropertyName("format")]
    public LuarcFormatConfig Format { get; set; } = new();
}
