using Silk.NET.Maths;

namespace Lilly.Engine.Rendering.Core.Primitives;

public class Transform3D
{
    public Vector3D<float> Position { get; set; } = Vector3D<float>.Zero;
    public Quaternion<float> Rotation { get; set; } = Quaternion<float>.Identity;
    public Vector3D<float> Scale { get; set; } = Vector3D<float>.One;

    public Matrix4X4<float> GetTransformationMatrix()
    {
        var translation = Matrix4X4.CreateTranslation(Position);
        var rotation = Matrix4X4.CreateFromQuaternion(Rotation);
        var scaling = Matrix4X4.CreateScale(Scale);
        return scaling * rotation * translation;
    }
}