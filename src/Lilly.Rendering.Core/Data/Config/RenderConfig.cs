namespace Lilly.Rendering.Core.Data.Config;

public class RenderConfig
{
    public RenderOpenGlApiLevel OpenGlApiLevel { get; set; } = new(3, 3);
    public RenderWindowConfig WindowConfig { get; set; } = new();

    public override string ToString()
        => $"RenderConfig: {{ WindowConfig: {WindowConfig} , OpenGlApiLevel: {OpenGlApiLevel} }}";
}
