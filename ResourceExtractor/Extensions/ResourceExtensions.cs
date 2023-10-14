namespace ResourceExtractor.Extensions;

using ResourceExtractor.Extractors;
using ResourceExtractor.Resources.Animation;
using ResourceExtractor.Resources.Dialog;
using ResourceExtractor.Resources.Label;
using ResourceExtractor.Resources.Spells;

using System.Text.Json;
using System.Text.Json.Serialization;

public static class ResourceExtensions {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        WriteIndented = true,
        Converters = {
            new JsonStringEnumConverter()
        }
    };

    public static string ToJson(this AnimationResource resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this AnimatorScript resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this Dialog resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this LabelSet resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this List<Spell> resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }
}