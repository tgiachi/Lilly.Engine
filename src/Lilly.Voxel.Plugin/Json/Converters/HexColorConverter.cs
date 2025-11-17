using System.Text.Json;
using System.Text.Json.Serialization;
using TrippyGL;

namespace Lilly.Voxel.Plugin.Json.Converters;

/// <summary>
/// Converts Color to and from a hexadecimal string in the format "#RRGGBB" or "#RRGGBBAA".
/// Examples: "#FFFFFF" -> Color.White, "#FF0000FF" -> Color.Red with full alpha
/// </summary>
public class HexColorConverter : JsonConverter<Color4b>
{
    public override Color4b Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string token for Color, got {reader.TokenType}");
        }

        var value = reader.GetString();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException("Color hex string cannot be null or empty");
        }

        // Remove leading # if present
        if (value.StartsWith('#'))
        {
            value = value[1..];
        }

        // Validate length (6 for RGB, 8 for RGBA)
        if (value.Length != 6 && value.Length != 8)
        {
            throw new JsonException($"Invalid hex color format: '#{value}'. Expected format: '#RRGGBB' or '#RRGGBBAA'");
        }

        try
        {
            // Parse hex string to integer
            int hexValue = int.Parse(value, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);

            if (value.Length == 6)
            {
                // RGB format: extract R, G, B and set alpha to 255
                int r = (hexValue >> 16) & 0xFF;
                int g = (hexValue >> 8) & 0xFF;
                int b = hexValue & 0xFF;
                return new Color4b(r, g, b, 255);
            }
            else
            {
                // RGBA format: extract R, G, B, A
                int r = (hexValue >> 24) & 0xFF;
                int g = (hexValue >> 16) & 0xFF;
                int b = (hexValue >> 8) & 0xFF;
                int a = hexValue & 0xFF;
                return new Color4b(r, g, b, a);
            }
        }
        catch (FormatException ex)
        {
            throw new JsonException($"Invalid hex color value: '#{value}'. Must contain only hexadecimal characters.", ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, Color4b value, JsonSerializerOptions options)
    {
        // Convert Color to hex string in format #RRGGBBAA
        string hexColor = $"#{value.R:X2}{value.G:X2}{value.B:X2}{value.A:X2}";
        writer.WriteStringValue(hexColor);
    }
}
