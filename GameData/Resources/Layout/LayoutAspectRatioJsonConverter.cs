namespace GameData.Resources.Layout;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Reads/writes <see cref="LayoutAspectRatio"/> as a bare JSON string ("10:9").</summary>
public class LayoutAspectRatioJsonConverter : JsonConverter<LayoutAspectRatio> {
    public override LayoutAspectRatio Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType != JsonTokenType.String) {
            throw new JsonException($"Expected an aspect-ratio string, found {reader.TokenType}.");
        }

        string? text = reader.GetString();
        if (!LayoutAspectRatio.TryParse(text, out LayoutAspectRatio result)) {
            throw new JsonException($"'{text}' is not an aspect ratio. Expected \"<width>:<height>\" with both parts positive.");
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, LayoutAspectRatio value, JsonSerializerOptions options) {
        writer.WriteStringValue(value.ToString());
    }
}
