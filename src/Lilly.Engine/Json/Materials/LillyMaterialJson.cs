using System.Numerics;
using System.Text.Json.Serialization;
using Lilly.Engine.Json.Converters;
using Lilly.Rendering.Core.Materials;
using TrippyGL;

namespace Lilly.Engine.Json.Materials;

/// <summary>
/// Serializable representation of <see cref="Material"/> for JSON files.
/// </summary>
public class LillyMaterialJson
{
    public string Name { get; set; } = string.Empty;
    public string ShaderName { get; set; } = string.Empty;
    public string AlbedoTexture { get; set; } = string.Empty;
    public string NormalTexture { get; set; } = string.Empty;
    public string RoughnessTexture { get; set; } = string.Empty;
    public string MetallicTexture { get; set; } = string.Empty;
    public string EmissiveTexture { get; set; } = string.Empty;
    public string AOTexture { get; set; } = string.Empty;

    [JsonConverter(typeof(HexColorConverter))]
    public Color4b Tint { get; set; } = new(255, 255, 255, 255);

    public float Roughness { get; set; } = 0.5f;
    public float Metallic { get; set; }

    [JsonConverter(typeof(Vector3Converter))]
    public Vector3 EmissiveColor { get; set; } = Vector3.Zero;

    public float EmissiveIntensity { get; set; }

    public bool IsTransparent { get; set; }

    public float AlphaThreshold { get; set; } = 0.5f;

    public bool CastShadows { get; set; } = true;
    public bool ReceiveShadows { get; set; } = true;

    [JsonConverter(typeof(Vector2Converter))]
    public Vector2 TextureScale { get; set; } = Vector2.One;

    [JsonConverter(typeof(Vector2Converter))]
    public Vector2 TextureOffset { get; set; } = Vector2.Zero;

    /// <summary>Creates a runtime <see cref="Material"/> with the values of this JSON definition.</summary>
    public Material ToMaterial()
    {
        return new Material
        {
            Name = Name,
            ShaderName = ShaderName,
            AlbedoTexture = AlbedoTexture,
            NormalTexture = NormalTexture,
            RoughnessTexture = RoughnessTexture,
            MetallicTexture = MetallicTexture,
            EmissiveTexture = EmissiveTexture,
            AOTexture = AOTexture,
            Tint = Tint,
            Roughness = Roughness,
            Metallic = Metallic,
            EmissiveColor = EmissiveColor,
            EmissiveIntensity = EmissiveIntensity,
            IsTransparent = IsTransparent,
            AlphaThreshold = AlphaThreshold,
            CastShadows = CastShadows,
            ReceiveShadows = ReceiveShadows,
            TextureScale = TextureScale,
            TextureOffset = TextureOffset
        };
    }

    /// <summary>Creates a JSON-friendly DTO from an existing <see cref="Material"/>.</summary>
    public static LillyMaterialJson FromMaterial(Material material)
    {
        ArgumentNullException.ThrowIfNull(material);

        return new LillyMaterialJson
        {
            Name = material.Name,
            ShaderName = material.ShaderName,
            AlbedoTexture = material.AlbedoTexture,
            NormalTexture = material.NormalTexture,
            RoughnessTexture = material.RoughnessTexture,
            MetallicTexture = material.MetallicTexture,
            EmissiveTexture = material.EmissiveTexture,
            AOTexture = material.AOTexture,
            Tint = material.Tint,
            Roughness = material.Roughness,
            Metallic = material.Metallic,
            EmissiveColor = material.EmissiveColor,
            EmissiveIntensity = material.EmissiveIntensity,
            IsTransparent = material.IsTransparent,
            AlphaThreshold = material.AlphaThreshold,
            CastShadows = material.CastShadows,
            ReceiveShadows = material.ReceiveShadows,
            TextureScale = material.TextureScale,
            TextureOffset = material.TextureOffset
        };
    }
}
