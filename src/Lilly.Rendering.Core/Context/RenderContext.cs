using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Lilly.Rendering.Core.Context;

public class RenderContext
{
    public GL OpenGl { get; init; }

    public IWindow Window { get; init; }
}
