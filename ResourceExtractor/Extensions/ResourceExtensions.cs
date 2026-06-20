namespace ResourceExtractor.Extensions;

using GameData.Resources.Animation;
using GameData.Resources.Book;
using GameData.Resources.Cursor;
using GameData.Resources.Data;
using GameData.Resources.Dialog;
using GameData.Resources.Monster;
using GameData.Resources.World;
using GameData.Resources.Image;
using GameData.Resources.Label;
using GameData.Resources.Location;
using GameData.Resources.Menu;
using GameData.Resources.Object;
using GameData.Resources.Palette;
using GameData.Resources.Scene;
using GameData.Resources.Spells;
using ResourceExtractor.Extractors.Container;
using ResourceExtractor.Imaging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Color = GameData.Resources.Palette.Color;

public static class ResourceExtensions {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        WriteIndented = true,
        Converters = {
            new JsonStringEnumConverter()
        }
    };

    public static string ToJson(this AnimatorResource resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this CursorSet resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this GameData.Resources.Creature.CreatureBitmaps resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this AnimationResource resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this Dialog resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this LabelSet resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this SpellList resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this SpellSymbolLayout resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this CastRing resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this List<ObjectInfo> resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this UserInterface resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this GdsScene resource) {
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

    public static string ToJson(this PaletteResource resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this RemapResource resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this KeywordList resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

public static string ToJson(this SaveGame resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this BmImage resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this ZoneBounds resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this ZoneDefinition resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this ZoneMap resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this ZoneRef resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this ZoneShape resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this WorldTile resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this TileEventTile resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this MonsterStats resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToJson(this ZoneTable resource) {
        return JsonSerializer.Serialize(resource, JsonOptions);
    }

    public static string ToCsv(this List<ObjectInfo> resource) {
        const string objectInfoFlags =
            $"{nameof(ObjectFlags.B0001)},{nameof(ObjectFlags.NotEquipable)},{nameof(ObjectFlags.B0004)},{nameof(ObjectFlags.B0008)},{nameof(ObjectFlags.B0010)},{nameof(ObjectFlags.B0020)},{nameof(ObjectFlags.OnlyUsableInCombat)},{nameof(ObjectFlags.B0080)},{nameof(ObjectFlags.NotUsableInCombat)},{nameof(ObjectFlags.ArchersOnly)},{nameof(ObjectFlags.B0400)},{nameof(ObjectFlags.Stackable)},{nameof(ObjectFlags.B1000)},{nameof(ObjectFlags.LimitedUses)},{nameof(ObjectFlags.B4000)},{nameof(ObjectFlags.B8000)}";
        var sb = new StringBuilder(
            $"{nameof(ObjectInfo.Number)},{nameof(ObjectInfo.Name)},{nameof(ObjectInfo.Field1E)},{objectInfoFlags},{nameof(ObjectInfo.WordWrap)},{nameof(ObjectInfo.ChapterNumber)},{nameof(ObjectInfo.Price)},{nameof(ObjectInfo.SwingBaseDamage)},{nameof(ObjectInfo.ThrustBaseDamage)},{nameof(ObjectInfo.SwingAccuracy_ArmorMod_BowAccuracy)},{nameof(ObjectInfo.ThrustAccuracy)},{nameof(ObjectInfo.Icon)},{nameof(ObjectInfo.InventorySlots)},{nameof(ObjectInfo.SoundId)},{nameof(ObjectInfo.MaxAmount)},{nameof(ObjectInfo.MaxCharges)},{nameof(ObjectInfo.Race)},{nameof(ObjectInfo.ShopType)},{nameof(ObjectInfo.Type)},{nameof(ObjectInfo.Attributes)},{nameof(ObjectInfo.UseEffectAttributeMask)},{nameof(ObjectInfo.UseEffectAmount)},{nameof(ObjectInfo.EffectDurationHours)},{nameof(ObjectInfo.EquipAttributeMask)},{nameof(ObjectInfo.EquipModifierAmount)},{nameof(ObjectInfo.DegradeChancePercent)},{nameof(ObjectInfo.MaxWearPerDegrade)},{nameof(ObjectInfo.MinimumQuality)}\r\n");
        foreach (ObjectInfo info in resource) {
            sb.AppendLine(info.ToCsv());
        }

        return sb.ToString();
    }

    public static string ToCsv(this SpellList resource) {
        var sb = new StringBuilder($"{nameof(Spell.Id)},{nameof(Spell.Name)},{nameof(Spell.MinimumCost)},{nameof(Spell.MaximumCost)},{nameof(Spell.IsMartial)},{nameof(Spell.TargetingType)},{nameof(Spell.Color)},{nameof(Spell.AnimationEffectType)},{nameof(Spell.ObjectId)},{nameof(Spell.Calculation)},{nameof(Spell.Damage)},{nameof(Spell.Duration)}\r\n");
        foreach (Spell spell in resource.Spells.Values) {
            sb.AppendLine(spell.ToCsv());
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

    public static RawImage ToRawImage(this BmImage image, Color[]? palette = null) {
        if (image.BitMapData == null) {
            throw new ArgumentException("Image data is null");
        }
        if (palette != null) {
            palette[0].A = 0;
        }
        var raw = new RawImage(image.Width, image.Height);

        int index = 0;
        if (image.Flags.HasFlag(ImageFlags.ReversedRowColumn)) {
            for (int x = 0; x < image.Width; x++) {
                for (int y = 0; y < image.Height; y++) {
                    PutPixel(raw, x, y, image.BitMapData[index++], palette);
                }
            }
        } else {
            for (int y = 0; y < image.Height; y++) {
                for (int x = 0; x < image.Width; x++) {
                    PutPixel(raw, x, y, image.BitMapData[index++], palette);
                }
            }
        }

        return raw;
    }

    private static void PutPixel(RawImage raw, int x, int y, byte colorIndex, Color[]? palette) {
        if (palette != null) {
            Color c = palette[colorIndex];
            raw.SetPixel(x, y, c.R, c.G, c.B, c.A);
        } else {
            raw.SetPixel(x, y, colorIndex, colorIndex, colorIndex, 255);
        }
    }
}