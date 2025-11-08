namespace Lilly.Engine.Rendering.Core.Data.Config;

public class InitialEngineOptions
{
    public string WindowTitle { get; set; } = "Squid Engine";
    public InitialGraphicOptions GraphicOptions { get; set; } = new();
    public GraphicApiVersion TargetRenderVersion { get; set; } = new(3, 3, 0, 0);
}
