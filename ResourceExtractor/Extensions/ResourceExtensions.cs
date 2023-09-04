namespace ResourceExtractor.Extensions;

using ResourceExtractor.Resources;

using System.Text.Json;
using System.Text.Json.Serialization;

public static class ResourceExtensions {
    public static string ToJson(this AnimationResource resource) {
        return JsonSerializer.Serialize(resource, new JsonSerializerOptions {
            WriteIndented = true,
            Converters = {
                new JsonStringEnumConverter()
            }
        });
    }    
    public static string ToJson(this AnimatorScript resource) {
        return JsonSerializer.Serialize(resource, new JsonSerializerOptions {
            WriteIndented = true,
            Converters = {
                new JsonStringEnumConverter()
            }
        });
    }
}