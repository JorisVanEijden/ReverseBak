namespace ResourceExtractor.Extensions;

using ResourceExtractor.Extractors;
using ResourceExtractor.Resources.Animation;
using ResourceExtractor.Resources.Dialog;
using ResourceExtractor.Resources.Label;
using ResourceExtractor.Resources.Spells;

using System.Text;
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

    public static string ToJson(this List<ObjectInfo> resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }    
    public static string ToCsv(this List<ObjectInfo> resource) {
        var sb = new StringBuilder("Number,Name,Field1E,Field20,Field22,Field24,Price,SwingBaseDamage,ThrustBaseDamage,SwingAccuracy_ArmorMod_BowAccuracy,ThrustAccuracy,Icon,Slots,Field34,Field36,Field37,Race,Field3A,Type,Field3E,Field40,Field42,Field44,CanEffect,Field48,Field4A,Field4C,Field4E\r\n");
        foreach (ObjectInfo info in resource) {
            sb.AppendLine(info.ToCsv());
        }
        return sb.ToString();
    }
}