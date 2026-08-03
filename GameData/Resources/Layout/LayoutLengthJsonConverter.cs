namespace GameData.Resources.Layout;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Reads/writes <see cref="LayoutLength"/> as a bare JSON string so layout data stays
/// hand-authorable ("50%" rather than {"Value":50,"Unit":"Percent"}).</summary>
public class LayoutLengthJsonConverter : JsonConverter<LayoutLength> {
    public override LayoutLength Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType != JsonTokenType.String) {
            throw new JsonException($"Expected a layout length string, found {reader.TokenType}.");
        }

        string? text = reader.GetString();
        if (!LayoutLength.TryParse(text, out LayoutLength result)) {
            throw new JsonException($"'{text}' is not a layout length. Expected \"<number>px\", \"<number>%\", or \"auto\".");
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, LayoutLength value, JsonSerializerOptions options) {
        writer.WriteStringValue(value.ToString());
    }
}
