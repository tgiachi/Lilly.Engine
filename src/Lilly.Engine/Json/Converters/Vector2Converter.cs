using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lilly.Engine.Json.Converters;

/// <summary>
/// Converts <see cref="Vector2"/> to and from JSON using either an array [x, y] or a string "x,y".
/// </summary>
public class Vector2Converter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.StartArray => ReadFromArray(ref reader),
            JsonTokenType.StartObject => ReadFromObject(ref reader),
            JsonTokenType.String => ReadFromString(ref reader),
            _ => throw new JsonException(
                $"Unexpected token parsing Vector2. Expected StartArray, StartObject or String, got {reader.TokenType}"
            )
        };
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteEndArray();
    }

    private static Vector2 ReadFromArray(ref Utf8JsonReader reader)
    {
        Span<float> values = stackalloc float[2];
        var index = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException($"Vector2 array values must be numbers, got {reader.TokenType}");
            }

            if (index >= 2)
            {
                throw new JsonException("Vector2 array must contain exactly 2 elements.");
            }

            values[index++] = reader.GetSingle();
        }

        if (index != 2 || reader.TokenType != JsonTokenType.EndArray)
        {
            throw new JsonException("Vector2 array must contain exactly 2 numeric values.");
        }

        return new(values[0], values[1]);
    }

    private static Vector2 ReadFromObject(ref Utf8JsonReader reader)
    {
        float? x = null;
        float? y = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException($"Expected property name while parsing Vector2, got {reader.TokenType}");
            }

            var propertyName = reader.GetString();

            if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException($"Expected numeric value for '{propertyName}' while parsing Vector2");
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
                default:
                    throw new JsonException($"Unexpected property '{propertyName}' while parsing Vector2");
            }
        }

        if (!x.HasValue || !y.HasValue)
        {
            throw new JsonException("Vector2 object must contain numeric x and y properties.");
        }

        return new(x.Value, y.Value);
    }

    private static Vector2 ReadFromString(ref Utf8JsonReader reader)
    {
        var value = reader.GetString();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException("Vector2 string cannot be null or empty.");
        }

        var parts = value.Split(
            new[] { ',', ';' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

        if (parts.Length != 2)
        {
            throw new JsonException($"Vector2 string must have 2 components separated by ',' or ';': '{value}'");
        }

        try
        {
            var x = float.Parse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture);
            var y = float.Parse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture);

            return new(x, y);
        }
        catch (FormatException ex)
        {
            throw new JsonException($"Invalid Vector2 string format: '{value}'", ex);
        }
    }
}
