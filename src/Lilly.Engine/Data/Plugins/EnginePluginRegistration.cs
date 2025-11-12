using System.Reflection;
using Lilly.Engine.Interfaces.Plugins;

namespace Lilly.Engine.Data.Plugins;

public record EnginePluginRegistration(Assembly Assembly, Type PluginType);

