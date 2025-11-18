using System.Text.Json;
using System.Text.Json.Serialization;
using Lilly.Voxel.Plugin.Primitives;
using Lilly.Voxel.Plugin.Types;

namespace Lilly.Voxel.Plugin.Json.Converters;

/// <summary>
/// Converts Dictionary&lt;BlockFace, BlockTextureObject&gt; with support for "all" key to set default texture for all faces.
/// Example: { "all": "texture@0", "top": "other@1" } will set all faces to "texture@0" except top which will be "other@1"
/// </summary>
public class BlockFaceDictionaryConverter : JsonConverter<Dictionary<BlockFace, BlockTextureObject>>
{
    private static readonly BlockTextureObjectConverter TextureConverter = new();

    public override Dictionary<BlockFace, BlockTextureObject> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Expected StartObject token, got {reader.TokenType}");
        }

        var result = new Dictionary<BlockFace, BlockTextureObject>();
        BlockTextureObject? defaultTexture = null;

        // First pass: read all entries and find "all" if present
        var entries = new Dictionary<string, BlockTextureObject>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException($"Expected PropertyName token, got {reader.TokenType}");
            }

            var propertyName = reader.GetString();

            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new JsonException("Property name cannot be null or empty");
            }

            reader.Read();

            // Parse the texture value
            var texture = TextureConverter.Read(ref reader, typeof(BlockTextureObject), options);

            if (propertyName.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                defaultTexture = texture;
            }
            else
            {
                entries[propertyName] = texture;
            }
        }

        // Apply default texture to all faces if specified
        if (defaultTexture.HasValue)
        {
            foreach (BlockFace face in Enum.GetValues<BlockFace>())
            {
                result[face] = defaultTexture.Value;
            }
        }

        // Apply specific face overrides
        foreach (var (key, texture) in entries)
        {
            if (Enum.TryParse<BlockFace>(key, true, out var face))
            {
                result[face] = texture;
            }
            else
            {
                throw new JsonException($"Invalid BlockFace name: '{key}'. Valid values are: {string.Join(", ", Enum.GetNames<BlockFace>())}");
            }
        }

        return result;
    }

    public override void Write(
        Utf8JsonWriter writer,
        Dictionary<BlockFace, BlockTextureObject> value,
        JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(value);

        writer.WriteStartObject();

        // Check if all faces have the same texture
        var allFaces = Enum.GetValues<BlockFace>();
        var firstTexture = value.Count > 0 ? value.Values.FirstOrDefault() : (BlockTextureObject?)null;
        var allSame = firstTexture.HasValue && value.Count == allFaces.Length && value.Values.All(t => t.Equals(firstTexture.Value));

        if (allSame && firstTexture.HasValue)
        {
            // All faces have the same texture, write as "all"
            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("all") ?? "all");
            TextureConverter.Write(writer, firstTexture.Value, options);
        }
        else
        {
            // Write individual faces
            foreach (var (face, texture) in value)
            {
                var propertyName = options.PropertyNamingPolicy?.ConvertName(face.ToString()) ?? face.ToString();
                writer.WritePropertyName(propertyName);
                TextureConverter.Write(writer, texture, options);
            }
        }

        writer.WriteEndObject();
    }
}
