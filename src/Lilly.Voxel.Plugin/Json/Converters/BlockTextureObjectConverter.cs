using System.Text.Json;
using System.Text.Json.Serialization;
using Squid.Engine.World.Voxels.Primitives;

namespace Lilly.Voxel.Plugin.Json.Converters;

/// <summary>
/// Converts BlockTextureObject to and from a string in the format "atlasName@index".
/// Example: "default_asset@1" -> BlockTextureObject("default_asset", 1)
/// </summary>
public class BlockTextureObjectConverter : JsonConverter<BlockTextureObject>
{
    public override BlockTextureObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string token, got {reader.TokenType}");
        }

        var value = reader.GetString();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException("BlockTextureObject string cannot be null or empty");
        }

        var parts = value.Split('@', 2);

        if (parts.Length != 2)
        {
            throw new JsonException($"Invalid BlockTextureObject format: '{value}'. Expected format: 'atlasName@index'");
        }

        var atlasName = parts[0];

        if (string.IsNullOrWhiteSpace(atlasName))
        {
            throw new JsonException($"Atlas name cannot be empty in BlockTextureObject: '{value}'");
        }

        if (!int.TryParse(parts[1], out var index))
        {
            throw new JsonException($"Invalid index in BlockTextureObject: '{parts[1]}'. Expected an integer.");
        }

        if (index < 0)
        {
            throw new JsonException($"Index must be non-negative in BlockTextureObject: {index}");
        }

        return new BlockTextureObject(atlasName, index);
    }

    public override void Write(Utf8JsonWriter writer, BlockTextureObject value, JsonSerializerOptions options)
    {
        if (string.IsNullOrWhiteSpace(value.AtlasName))
        {
            throw new JsonException("BlockTextureObject.AtlasName cannot be null or empty");
        }

        if (value.Index < 0)
        {
            throw new JsonException($"BlockTextureObject.Index must be non-negative: {value.Index}");
        }

        var stringValue = $"{value.AtlasName}@{value.Index}";
        writer.WriteStringValue(stringValue);
    }
}
