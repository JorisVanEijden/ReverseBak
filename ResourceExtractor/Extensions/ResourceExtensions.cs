namespace ResourceExtractor.Extensions;

using GameData.Resources.Animation;
using GameData.Resources.Book;
using GameData.Resources.Dialog;
using GameData.Resources.Label;
using GameData.Resources.Location;
using GameData.Resources.Menu;
using GameData.Resources.Object;
using GameData.Resources.Spells;

using ResourceExtractor.Extractors.Container;

using System.Drawing;
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

    public static string ToJson(this UserInterface resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }
    
    public static string ToJson(this Color[] resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this BookResource resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }
    
    public static string ToJson(this List<Container> resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }
    
    public static string ToJson(this List<TeleportDestination> resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToCsv(this List<ObjectInfo> resource) {
        const string objectInfoFlags =
            $"{nameof(ObjectFlags.B0001)},{nameof(ObjectFlags.NotEquipable)},{nameof(ObjectFlags.B0004)},{nameof(ObjectFlags.B0008)},{nameof(ObjectFlags.B0010)},{nameof(ObjectFlags.B0020)},{nameof(ObjectFlags.OnlyUsableInCombat)},{nameof(ObjectFlags.B0080)},{nameof(ObjectFlags.NotUsableInCombat)},{nameof(ObjectFlags.ArchersOnly)},{nameof(ObjectFlags.B0400)},{nameof(ObjectFlags.Stackable)},{nameof(ObjectFlags.B1000)},{nameof(ObjectFlags.LimitedUses)},{nameof(ObjectFlags.B4000)},{nameof(ObjectFlags.B8000)}";
        var sb = new StringBuilder(
            $"{nameof(ObjectInfo.Number)},{nameof(ObjectInfo.Name)},{nameof(ObjectInfo.Field1E)},{objectInfoFlags},{nameof(ObjectInfo.WordWrap)},{nameof(ObjectInfo.ChapterNumber)},{nameof(ObjectInfo.Price)},{nameof(ObjectInfo.SwingBaseDamage)},{nameof(ObjectInfo.ThrustBaseDamage)},{nameof(ObjectInfo.SwingAccuracy_ArmorMod_BowAccuracy)},{nameof(ObjectInfo.ThrustAccuracy)},{nameof(ObjectInfo.Icon)},{nameof(ObjectInfo.InventorySlots)},{nameof(ObjectInfo.SoundId)},{nameof(ObjectInfo.MaxAmount)},{nameof(ObjectInfo.Field37)},{nameof(ObjectInfo.Race)},{nameof(ObjectInfo.ShopType)},{nameof(ObjectInfo.Type)},{nameof(ObjectInfo.Attributes)},{nameof(ObjectInfo.Field40)},{nameof(ObjectInfo.Field42)},{nameof(ObjectInfo.Book1Potion8)},{nameof(ObjectInfo.CanEffect)},{nameof(ObjectInfo.Field48)},{nameof(ObjectInfo.Field4A)},{nameof(ObjectInfo.Field4C)},{nameof(ObjectInfo.Field4E)}\r\n");
        foreach (ObjectInfo info in resource) {
            sb.AppendLine(info.ToCsv());
        }
        return sb.ToString();
    }

    public static string ToCsv(this List<Spell> resource) {
        var sb = new StringBuilder($"{nameof(Spell.Id)},{nameof(Spell.Name)},{nameof(Spell.MinimumCost)},{nameof(Spell.MaximumCost)},{nameof(Spell.Field6)},{nameof(Spell.Field8)},{nameof(Spell.FieldA)},{nameof(Spell.FieldC)},{nameof(Spell.ObjectId)},{nameof(Spell.Calculation)},{nameof(Spell.Damage)},{nameof(Spell.Duration)}\r\n");
        foreach (Spell info in resource) {
            sb.AppendLine(info.ToCsv());
        }
        return sb.ToString();
    }
    public static string ToCsv(this Color[] resource) {
        var sb = new StringBuilder($"index,hex,color\r\n");
        for (var index = 0; index < resource.Length; index++) {
            Color color = resource[index];
            sb.AppendLine($"{index},{index:X2},{color.R:X2}{color.G:X2}{color.B:X2}");
        }
        return sb.ToString();
    }
}