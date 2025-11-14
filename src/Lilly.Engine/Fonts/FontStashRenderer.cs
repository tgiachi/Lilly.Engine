using System.Drawing;
using System.Numerics;
using FontStashSharp;
using FontStashSharp.Interfaces;
using Lilly.Engine.Extensions;
using TrippyGL;

namespace Lilly.Engine.Fonts;

/// <summary>
/// Provides rendering capabilities for FontStash font rendering using OpenGL.
/// </summary>
public class FontStashRenderer : IFontStashRenderer, IDisposable
{
    public TextureBatcher SpriteBatcher { get; set; }

    private readonly Texture2DManager _textureManager;
    private SimpleShaderProgram _shaderProgram;

    /// <summary>
    /// Gets the texture manager for font texture operations.
    /// </summary>
    public ITexture2DManager TextureManager => _textureManager;

    /// <summary>
    /// Gets the graphics device used for rendering.
    /// </summary>
    public GraphicsDevice GraphicsDevice => _textureManager.GraphicsDevice;

    /// <summary>
    /// Initializes a new instance of the <see cref="FontStashRenderer" /> class.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device to use for rendering operations.</param>
    public FontStashRenderer(GraphicsDevice graphicsDevice)
        => _textureManager = new(graphicsDevice);

    /// <summary>
    /// Begins a new batch rendering session.
    /// </summary>
    public void Begin() { }

    /// <summary>
    /// Disposes the shader program, texture batcher, and releases resources.
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Draws a texture at the specified position with the given properties.
    /// </summary>
    /// <param name="texture">The texture to draw.</param>
    /// <param name="pos">The position to draw the texture at.</param>
    /// <param name="src">The source rectangle within the texture, or null for the entire texture.</param>
    /// <param name="color">The color to tint the texture with.</param>
    /// <param name="rotation">The rotation angle in radians.</param>
    /// <param name="scale">The scale factor to apply.</param>
    /// <param name="depth">The depth value for layering.</param>
    public void Draw(
        object texture,
        Vector2 pos,
        Rectangle? src,
        FSColor color,
        float rotation,
        Vector2 scale,
        float depth
    )
    {
        var tex = (Texture2D)texture;

        SpriteBatcher.Draw(
            tex,
            pos,
            src,
            color.ToTrippy(),
            scale,
            rotation,
            Vector2.Zero,
            depth
        );
    }

    /// <summary>
    /// Ends the current batch rendering session and flushes all queued draw calls.
    /// </summary>
    public void End() { }

    /// <summary>
    /// Sets the shader program to use for rendering and updates the projection matrix.
    /// </summary>
    /// <param name="shaderProgram">The shader program to use.</param>
    public void SetShaderProgram(SimpleShaderProgram shaderProgram)
    {
        _shaderProgram = shaderProgram;
    }
}
