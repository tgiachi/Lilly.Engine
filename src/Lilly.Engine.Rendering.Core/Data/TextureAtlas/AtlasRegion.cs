using Silk.NET.Maths;

namespace Lilly.Engine.Rendering.Core.Data.TextureAtlas;

public record struct AtlasRegion(
    Vector2D<float> Position,
    Vector2D<float> Size
);

