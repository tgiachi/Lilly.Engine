
using Silk.NET.Maths;

namespace Lilly.Engine.Rendering.Core.Data.Config;

public class InitialGraphicOptions
{
    public bool VSync { get; set; } = true;
    public Vector2D<int> WindowSize { get; set; } = new(800, 600);
}
