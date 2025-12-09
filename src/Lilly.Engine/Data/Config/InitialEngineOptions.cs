using Lilly.Rendering.Core.Data.Config;

namespace Lilly.Engine.Data.Config;

public class InitialEngineOptions
{
    public RenderConfig RenderConfig { get; set; } = new();
}
