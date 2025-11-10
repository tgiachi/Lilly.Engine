using System.Text.Json.Serialization;
using Lilly.Engine.Lua.Scripting.Data;

namespace Lilly.Engine.Lua.Scripting.Context;

[JsonSerializable(typeof(LuarcConfig)), JsonSerializable(typeof(LuarcRuntimeConfig)),
 JsonSerializable(typeof(LuarcWorkspaceConfig)), JsonSerializable(typeof(LuarcDiagnosticsConfig)),
 JsonSerializable(typeof(LuarcCompletionConfig)), JsonSerializable(typeof(LuarcFormatConfig))]

/// <summary>
/// JSON serialization context for Lua scripting configuration types.
/// </summary>
public partial class LillyLuaScriptJsonContext : JsonSerializerContext { }
