using Silk.NET.OpenGL;
using TrippyGL;
using TextureMagFilter = Silk.NET.OpenGL.TextureMagFilter;
using TextureMinFilter = TrippyGL.TextureMinFilter;
using TextureWrapMode = Silk.NET.OpenGL.TextureWrapMode;

namespace Lilly.Rendering.Core.Lights;

/// <summary>
/// Framebuffer for shadow mapping (depth buffer only)
/// </summary>
public class ShadowFramebuffer : IDisposable
{
    private readonly GL _gl;
    private readonly uint _framebufferId;
    private bool _disposed;

    /// <summary>
    /// Depth texture containing shadow map data
    /// </summary>
    public Texture2D DepthTexture { get; }

    /// <summary>
    /// Width of the shadow map in pixels
    /// </summary>
    public uint Width { get; }

    /// <summary>
    /// Height of the shadow map in pixels
    /// </summary>
    public uint Height { get; }

    /// <summary>
    /// Creates a new shadow framebuffer with depth texture
    /// </summary>
    /// <param name="gl">OpenGL context</param>
    /// <param name="device">Graphics device</param>
    /// <param name="width">Width of shadow map</param>
    /// <param name="height">Height of shadow map</param>
    public ShadowFramebuffer(GL gl, GraphicsDevice device, uint width, uint height)
    {
        _gl = gl;
        Width = width;
        Height = height;

        // Create depth texture
        DepthTexture = new(
            device,
            width,
            height,
            generateMipmaps: false,
            samples: 0,
            imageFormat: TextureImageFormat.Depth24
        );

        // Configure texture filtering for shadow sampling
        _gl.BindTexture(TextureTarget.Texture2D, DepthTexture.Handle);
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Linear
        );
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear
        );
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.ClampToEdge
        );
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToEdge
        );
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureCompareMode,
            (int)TextureCompareMode.CompareRefToTexture
        );
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureCompareFunc,
            (int)GLEnum.Lequal
        );

        // Create framebuffer
        _framebufferId = _gl.GenFramebuffer();
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _framebufferId);

        // Attach only depth
        _gl.FramebufferTexture2D(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment,
            TextureTarget.Texture2D,
            DepthTexture.Handle,
            0
        );

        // Tell OpenGL not to draw to color buffer
        _gl.DrawBuffer(DrawBufferMode.None);
        _gl.ReadBuffer(ReadBufferMode.None);

        // Verify framebuffer is complete
        var status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);

        if (status != GLEnum.FramebufferComplete)
        {
            throw new InvalidOperationException($"Shadow framebuffer is not complete: {status}");
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    /// <summary>
    /// Bind this framebuffer for rendering to shadow map
    /// </summary>
    public void Bind()
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _framebufferId);
        _gl.Viewport(0, 0, Width, Height);
    }

    /// <summary>
    /// Unbind this framebuffer and return to screen rendering
    /// </summary>
    public void Unbind()
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        DepthTexture?.Dispose();

        if (_framebufferId != 0)
        {
            _gl.DeleteFramebuffer(_framebufferId);
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
