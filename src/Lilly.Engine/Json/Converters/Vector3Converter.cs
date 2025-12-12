using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lilly.Engine.Json.Converters;

/// <summary>
/// Converts <see cref="Vector3"/> to and from JSON using either an array [x, y, z] or a string "x,y,z".
/// </summary>
public class Vector3Converter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.StartArray => ReadFromArray(ref reader),
            JsonTokenType.StartObject => ReadFromObject(ref reader),
            JsonTokenType.String => ReadFromString(ref reader),
            _ => throw new JsonException(
                $"Unexpected token parsing Vector3. Expected StartArray, StartObject or String, got {reader.TokenType}"
            )
        };
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);
        writer.WriteEndArray();
    }

    private static Vector3 ReadFromArray(ref Utf8JsonReader reader)
    {
        Span<float> values = stackalloc float[3];
        var index = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException($"Vector3 array values must be numbers, got {reader.TokenType}");
            }

            if (index >= 3)
            {
                throw new JsonException("Vector3 array must contain exactly 3 elements.");
            }

            values[index++] = reader.GetSingle();
        }

        if (index != 3 || reader.TokenType != JsonTokenType.EndArray)
        {
            throw new JsonException("Vector3 array must contain exactly 3 numeric values.");
        }

        return new(values[0], values[1], values[2]);
    }

    private static Vector3 ReadFromObject(ref Utf8JsonReader reader)
    {
        float? x = null;
        float? y = null;
        float? z = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException($"Expected property name while parsing Vector3, got {reader.TokenType}");
            }

            var propertyName = reader.GetString();

            if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException($"Expected numeric value for '{propertyName}' while parsing Vector3");
            }

            var value = reader.GetSingle();

            switch (propertyName?.ToLowerInvariant())
            {
                case "x":
                    x = value;
                    break;
                case "y":
                    y = value;
                    break;
                case "z":
                    z = value;
                    break;
                default:
                    throw new JsonException($"Unexpected property '{propertyName}' while parsing Vector3");
            }
        }

        if (!x.HasValue || !y.HasValue || !z.HasValue)
        {
            throw new JsonException("Vector3 object must contain numeric x, y and z properties.");
        }

        return new(x.Value, y.Value, z.Value);
    }

    private static Vector3 ReadFromString(ref Utf8JsonReader reader)
    {
        var value = reader.GetString();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException("Vector3 string cannot be null or empty.");
        }

        var parts = value.Split(
            new[] { ',', ';' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

        if (parts.Length != 3)
        {
            throw new JsonException($"Vector3 string must have 3 components separated by ',' or ';': '{value}'");
        }

        try
        {
            var x = float.Parse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture);
            var y = float.Parse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture);
            var z = float.Parse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture);

            return new(x, y, z);
        }
        catch (FormatException ex)
        {
            throw new JsonException($"Invalid Vector3 string format: '{value}'", ex);
        }
    }
}
