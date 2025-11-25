using Silk.NET.Maths;

namespace Lilly.Engine.Data.Atlas;

public record struct AtlasRegion(
    Vector2D<float> Position,
    Vector2D<float> Size
);

