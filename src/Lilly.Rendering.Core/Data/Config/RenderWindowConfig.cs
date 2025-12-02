namespace Lilly.Rendering.Core.Data.Config;

public class RenderWindowConfig
{
    public int Width { get; set; }
    public int Height { get; set; }
    public bool Fullscreen { get; set; }
    public string Title { get; set; }
    public bool VSync { get; set; }

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
