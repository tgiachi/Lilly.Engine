namespace Lilly.Rendering.Core.Data.Config;

public record RenderOpenGlApiLevel(int Major, int Minor)
{
    public override string ToString()
        => $"{Major}.{Minor}";
}
