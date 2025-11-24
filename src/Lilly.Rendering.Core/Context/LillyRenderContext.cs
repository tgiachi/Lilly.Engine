using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Lilly.Rendering.Core.Context;

public class LillyRenderContext
{
    public GL OpenGl { get; init; }

    public IWindow Window { get; init; }
}
