namespace Lilly.Rendering.Core.Data.Config;

public class RenderWindowConfig
{
    public int Width { get; init; }
    public int Height { get; init; }
    public bool Fullscreen { get; init; }
    public string Title { get; init; }
    public bool VSync { get; init; }

    public int MaxFramerate { get; init; } = 70;

    public RenderWindowConfig(int width, int height, bool fullscreen, string title, bool vSync)
    {
        Width = width;
        Height = height;
        Fullscreen = fullscreen;
        Title = title;
        VSync = vSync;

    }

    public RenderWindowConfig() : this(1280, 768, false, "Squid Lilly Engine", true) { }

    public override string ToString()
        => $"RenderWindowConfig: {{ Width: {Width}, Height: {Height}, Fullscreen: {Fullscreen}, Title: {Title}, VSync: {VSync}, MaxFramerate: {MaxFramerate} }}";
}
