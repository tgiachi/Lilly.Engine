using Silk.NET.Maths;

namespace Lilly.Engine.Rendering.Core.Payloads;

public readonly struct ScissorPayload
{
    public bool IsEnabled { get; init; }
    public int X { get; init; }

    public int Y { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }

    public ScissorPayload(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        IsEnabled = true;
    }

    public ScissorPayload(Rectangle<int> rect)
    {
        X = rect.Origin.X;
        Y = rect.Origin.Y;
        Width = rect.Size.X;
        Height = rect.Size.Y;
        IsEnabled = true;
    }

    /// <summary>
    /// Default constructor initializing an empty scissor payload.
    /// </summary>
    public ScissorPayload( )
    {
        IsEnabled = false;
        X = -1;
        Y = -1;
        Width = -1;
        Height = -1;
    }

    public override string ToString()
    {
        return $"ScissorPayload(X: {X}, Y: {Y}, Width: {Width}, Height: {Height})";
    }
}
