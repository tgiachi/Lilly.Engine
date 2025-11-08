using Lilly.Engine.Core.Data.Privimitives;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using TrippyGL;

namespace Lilly.Engine.Rendering.Core.Contexts;

public class RenderContext
{
    public IWindow Window { get; set; }
    public GL Gl { get; set; }
    public GraphicsDevice GraphicsDevice { get; set; }

    public GameTime GameTime { get; set; } = new();

    public IInputContext InputContext { get; set; }

}
