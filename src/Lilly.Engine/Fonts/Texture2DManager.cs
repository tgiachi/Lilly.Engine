using System.Drawing;
using FontStashSharp.Interfaces;
using Silk.NET.OpenGL;
using TrippyGL;

namespace Lilly.Engine.Fonts;

/// <summary>
/// Manages 2D textures for font rendering operations.
/// </summary>
internal class Texture2DManager : ITexture2DManager
{
    /// <summary>
    /// Gets the graphics device used for texture operations.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Texture2DManager" /> class.
    /// </summary>
    /// <param name="device">The graphics device to use for texture operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when device is null.</exception>
    public Texture2DManager(GraphicsDevice device)
        => GraphicsDevice = device ?? throw new ArgumentNullException(nameof(device));

    /// <summary>
    /// Creates a new 2D texture with the specified dimensions.
    /// </summary>
    /// <param name="width">The width of the texture in pixels.</param>
    /// <param name="height">The height of the texture in pixels.</param>
    /// <returns>A new Texture2D object.</returns>
    public object CreateTexture(int width, int height)
        => new Texture2D(GraphicsDevice, (uint)width, (uint)height);

    /// <summary>
    /// Gets the size of a texture.
    /// </summary>
    /// <param name="texture">The texture to query.</param>
    /// <returns>A Point containing the width and height of the texture.</returns>
    public Point GetTextureSize(object texture)
    {
        var xnaTexture = (Texture2D)texture;

        return new((int)xnaTexture.Width, (int)xnaTexture.Height);
    }

    /// <summary>
    /// Sets pixel data for a region of a texture.
    /// </summary>
    /// <param name="texture">The texture to update.</param>
    /// <param name="bounds">The rectangular region to update.</param>
    /// <param name="data">The pixel data to set.</param>
    public void SetTextureData(object texture, Rectangle bounds, byte[] data)
    {
        var xnaTexture = (Texture2D)texture;

        xnaTexture.SetData<byte>(data, bounds.X, bounds.Y, (uint)bounds.Width, (uint)bounds.Height, PixelFormat.Rgba);
    }
}
