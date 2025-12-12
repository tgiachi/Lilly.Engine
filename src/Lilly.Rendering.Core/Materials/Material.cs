using System.Numerics;
using TrippyGL;

namespace Lilly.Rendering.Core.Materials;

public class Material
{
    public string Name { get; set; }
    public string ShaderName { get; set; }
    public string AlbedoTexture { get; set; }
    public string NormalTexture { get; set; }
    public string RoughnessTexture { get; set; }
    public string MetallicTexture { get; set; }
    public string EmissiveTexture { get; set; }
    public string AOTexture { get; set; }

    public Color4b Tint { get; set; } = Vector4.One;
    public float Roughness { get; set; } = 0.5f;
    public float Metallic { get; set; }
    public Vector3 EmissiveColor { get; set; } = Vector3.Zero;
    public float EmissiveIntensity { get; set; }

    public bool IsTransparent { get; set; }

    public float AlphaThreshold { get; set; } = 0.5f; // Per alpha clipping

    public bool CastShadows { get; set; } = true;
    public bool ReceiveShadows { get; set; } = true;

    public Vector2 TextureScale { get; set; } = Vector2.One;

    public Vector2 TextureOffset { get; set; } = Vector2.Zero;

    public bool HaveAlbedoTexture => !string.IsNullOrEmpty(AlbedoTexture);
    public bool HaveNormalTexture => !string.IsNullOrEmpty(NormalTexture);
    public bool HaveRoughnessTexture => !string.IsNullOrEmpty(RoughnessTexture);
    public bool HaveMetallicTexture => !string.IsNullOrEmpty(MetallicTexture);
    public bool HaveEmissiveTexture => !string.IsNullOrEmpty(EmissiveTexture);
    public bool HaveAOTexture => !string.IsNullOrEmpty(AOTexture);

    public Material Clone()
    {
        return (Material)MemberwiseClone();
    }

    public Material CreateInstance(string instanceName)
    {
        var instance = Clone();
        instance.Name = instanceName;
        return instance;
    }

    public override string ToString()
    {
        return $"Material: {Name}, Shader: {ShaderName}";
    }

    public void  Apply(GraphicsDevice graphicsDevice)
    {

    }
}
