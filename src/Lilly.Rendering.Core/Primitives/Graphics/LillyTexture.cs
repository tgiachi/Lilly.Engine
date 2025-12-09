using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Lilly.Rendering.Core.Primitives.Graphics;

public class LillyTexture : IDisposable
{
    public uint Handle { get; private set; }
    private readonly GL _gl;
    private bool disposed;

    private readonly int _wrapS;
    private readonly int _wrapT;
    private readonly int _minFilter;
    private readonly int _magFilter;
    private readonly bool _generateMipMaps;
    private readonly bool _srgb;

    public unsafe LillyTexture(
        GL gl,
        string path,
        int wrapS = (int)GLEnum.ClampToEdge,
        int wrapT = (int)GLEnum.ClampToEdge,
        int minFilter = (int)GLEnum.Nearest,
        int magFilter = (int)GLEnum.Nearest,
        bool generateMipMaps = true,
        bool srgb = false
    )
    {
        _gl = gl;
        _wrapS = wrapS;
        _wrapT = wrapT;
        _minFilter = minFilter;
        _magFilter = magFilter;
        _generateMipMaps = generateMipMaps;
        _srgb = srgb;

        Handle = _gl.GenTexture();
        Bind();

        using (var img = Image.Load<Rgba32>(path))
        {
            img.Mutate(x => x.Flip(FlipMode.Vertical));

            gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                srgb ? InternalFormat.Srgb8Alpha8 : InternalFormat.Rgba8,
                (uint)img.Width,
                (uint)img.Height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                null
            );

            img.ProcessPixelRows(
                accessor =>
                {
                    for (var y = 0; y < accessor.Height; y++)
                    {
                        fixed (void* data = accessor.GetRowSpan(y))
                        {
                            gl.TexSubImage2D(
                                TextureTarget.Texture2D,
                                0,
                                0,
                                y,
                                (uint)accessor.Width,
                                1,
                                PixelFormat.Rgba,
                                PixelType.UnsignedByte,
                                data
                            );
                        }
                    }
                }
            );
        }
        SetParameters();
    }

    public unsafe LillyTexture(
        GL gl,
        ReadOnlySpan<byte> data,
        uint width,
        uint height,
        bool generateMipMaps = true,
        bool srgb = false,
        int wrapS = (int)GLEnum.ClampToEdge,
        int wrapT = (int)GLEnum.ClampToEdge,
        int minFilter = (int)GLEnum.Nearest,
        int magFilter = (int)GLEnum.Nearest
    )
    {
        _gl = gl;
        _wrapS = wrapS;
        _wrapT = wrapT;
        _minFilter = minFilter;
        _magFilter = magFilter;
        _generateMipMaps = generateMipMaps;
        _srgb = srgb;

        var expectedBytes = checked(width * height * 4);

        if (data.Length < expectedBytes)
        {
            throw new ArgumentException(
                $"Data length {data.Length} is less than expected {expectedBytes} for {width}x{height} RGBA texture."
            );
        }

        Handle = _gl.GenTexture();
        Bind();

        fixed (void* d = data)
        {
            _gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                (int)(srgb ? InternalFormat.Srgb8Alpha8 : InternalFormat.Rgba),
                width,
                height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                d
            );
            SetParameters();
        }
    }

    public void Bind(TextureUnit textureSlot = TextureUnit.Texture0)
    {
        //When we bind a texture we can choose which textureslot we can bind it to.
        _gl.ActiveTexture(textureSlot);
        _gl.BindTexture(TextureTarget.Texture2D, Handle);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                if (Handle != 0)
                {
                    _gl.DeleteTexture(Handle);
                    Handle = 0;
                }
            }

            disposed = true;
        }
    }

    private void SetParameters()
    {
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, _wrapS);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, _wrapT);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, _minFilter);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, _magFilter);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, _generateMipMaps ? 8 : 0);

        if (_generateMipMaps)
        {
            _gl.GenerateMipmap(TextureTarget.Texture2D);
        }
    }
}
