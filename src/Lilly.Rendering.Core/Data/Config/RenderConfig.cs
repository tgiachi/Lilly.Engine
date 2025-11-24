namespace Lilly.Rendering.Core.Data.Config;

public class RenderConfig
{
    public RenderOpenGlApiLevel OpenGlApiLevel { get; init; } = new(3, 3);
    public RenderWindowConfig WindowConfig { get; init; } = new();

    public override string ToString()
        => $"RenderConfig: {{ WindowConfig: {WindowConfig} , OpenGlApiLevel: {OpenGlApiLevel} }}";
}
