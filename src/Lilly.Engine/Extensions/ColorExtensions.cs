using System.Numerics;
using FontStashSharp;
using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Engine.Extensions;

/// <summary>
/// Provides extension methods for converting between color and geometric types.
/// </summary>
public static class ColorExtensions
{
    /// <summary>
    /// Converts a TrippyGL Viewport to a System.Drawing.Rectangle.
    /// </summary>
    /// <param name="r">The viewport to convert.</param>
    /// <returns>A Rectangle with the dimensions of the viewport.</returns>
    public static System.Drawing.Rectangle ToSystemDrawing(this Viewport r)
        => new(r.X, r.Y, (int)r.Width, (int)r.Height);

    /// <summary>
    /// Converts a System.Drawing.Point to a System.Numerics.Vector2.
    /// </summary>
    /// <param name="p">The point to convert.</param>
    /// <returns>A Vector2 with the X and Y coordinates of the point.</returns>
    public static Vector2 ToSystemNumeric(System.Drawing.Point p)
        => new(p.X, p.Y);

    /// <summary>
    /// Converts a System.Drawing.Rectangle to a TrippyGL Viewport.
    /// </summary>
    /// <param name="r">The rectangle to convert.</param>
    /// <returns>A Viewport with the dimensions of the rectangle.</returns>
    public static Viewport ToTrippy(this System.Drawing.Rectangle r)
        => new(r);

    /// <summary>
    /// Converts a FontStashSharp color to a TrippyGL Color4b.
    /// </summary>
    /// <param name="c">The FontStashSharp color to convert.</param>
    /// <returns>A Color4b with the RGBA components of the source color.</returns>
    public static Color4b ToTrippy(this FSColor c)
        => new(c.R, c.G, c.B, c.A);

    /// <summary>
    /// Converts a Silk.NET Rectangle to a System.Drawing.Rectangle.
    /// </summary>
    /// <param name="r">The Silk.NET rectangle to convert.</param>
    /// <returns>A System.Drawing.Rectangle with the dimensions of the source rectangle.</returns>
    public static System.Drawing.Rectangle ToSystemDrawing(this Rectangle<float> r)
        => new((int)r.Origin.X, (int)r.Origin.Y, (int)r.Size.X, (int)r.Size.Y);
}
