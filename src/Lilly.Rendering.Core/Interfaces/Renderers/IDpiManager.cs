using System.Numerics;

namespace Lilly.Rendering.Core.Interfaces.Renderers;

public interface IDpiManager
{
    Vector2 WindowSize { get; }

    Vector2 FramebufferSize { get; }

    float DPIScale { get; }

    Matrix4x4 GetProjectionMatrix();

    float ScaleDimension(float logicalSize);

    void UpdateSizes();
}
