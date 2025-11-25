using Lilly.Rendering.Core.Interfaces.Renderers;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using TrippyGL;

namespace Lilly.Rendering.Core.Context;

public class RenderContext
{
    public GL OpenGl { get; set; }

    public IWindow Window { get; set; }

    public IInputContext Input { get; set; }

    public GraphicsDevice GraphicsDevice { get; set; }

    public IGraphicRenderer Renderer { get; set; }


    public IDpiManager DpiManager { get; set; }
}
