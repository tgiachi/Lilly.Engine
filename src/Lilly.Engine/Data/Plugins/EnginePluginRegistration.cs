using System.Reflection;

namespace Lilly.Engine.Data.Plugins;

public record EnginePluginRegistration(Assembly Assembly, Type PluginType);
