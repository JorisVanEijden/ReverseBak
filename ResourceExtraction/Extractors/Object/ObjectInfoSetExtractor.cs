namespace ResourceExtraction.Extractors.Object;

using GameData;
using GameData.Resources.Object;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// Parses OBJINFO.DAT — 138 fixed 80-byte item-definition records — into an
/// <see cref="ObjectInfoSet"/> indexable by object id. Field order/widths mirror
/// <c>ResourceExtractor.Extractors.ObjectExtractor</c> verbatim (the console-tool
/// extractor that predates this Unity-facing one); see <see cref="ObjectInfo"/> for
/// the per-field IDA references.
/// </summary>
public class ObjectInfoSetExtractor : ExtractorBase<ObjectInfoSet> {
    private const int RecordCount = 138;

    /// <summary>
    /// Scroll prices follow the records — 45 shorts, one per spell, which is exactly the 90 bytes
    /// OBJINFO.DAT carries past 138 x 80 = 11040 = 0x2b20, and exactly the number of spells
    /// SPELLDOC ships. See <see cref="ObjectInfoSet.SpellPrices"/>.
    /// </summary>
    private const int SpellPriceCount = 45;

    /// <summary>Bytes per item record — the stride that puts the spell table at 0x2b20.</summary>
    private const int RecordSize = 80;

    public override ObjectInfoSet Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));

        var items = new List<ObjectInfo>(RecordCount);
        for (int i = 0; i < RecordCount; i++) {
            var objectInfo = new ObjectInfo(id) {
                Number = i,
                Name = new string(reader.ReadChars(30)).Replace('\0', ' ').Trim(),
                Field1E = reader.ReadUInt16(),
                Flags = (ObjectFlags)reader.ReadUInt16(),
                WordWrap = reader.ReadUInt16(),
                ChapterNumber = reader.ReadUInt16(),
                Price = reader.ReadUInt16(),
                SwingBaseDamage = reader.ReadInt16(),
                ThrustBaseDamage = reader.ReadInt16(),
                SwingAccuracy_ArmorMod_BowAccuracy = reader.ReadInt16(),
                ThrustAccuracy = reader.ReadInt16(),
                Icon = reader.ReadUInt16(),
                InventorySlots = reader.ReadUInt16(),
                SoundId = reader.ReadByte(),
                SoundRepeat = reader.ReadByte(),
                MaxAmount = reader.ReadByte(),
                MaxCharges = reader.ReadByte(),               // +0x37 limited-use max charges
                Race = (Race)reader.ReadUInt16(),
                ShopType = reader.ReadUInt16(),
                ObjectType = (ObjectType)reader.ReadUInt16(),
                EffectArgA = reader.ReadUInt16(),               // +0x3E per-category effect arg A
                EffectArgB = reader.ReadUInt16(),               // +0x40 per-category effect arg B
                UseEffectAmount = reader.ReadUInt16(),        // +0x42 effect magnitude
                EffectDurationHours = reader.ReadUInt16(),    // +0x44 timed-effect duration (hours)
                EquipAttributeMask = (ActorAttributeFlag)reader.ReadUInt16(), // +0x46 passive-modifier attribute mask
                EquipModifierAmount = reader.ReadInt16(),     // +0x48 passive-modifier amount (signed)
                DegradeChancePercent = reader.ReadUInt16(),   // +0x4A
                MaxWearPerDegrade = reader.ReadUInt16(),      // +0x4C
                MinimumQuality = reader.ReadUInt16()          // +0x4E
            };

            items.Add(objectInfo);
        }

        // *** SEEK, DO NOT TRUST THE POSITION. *** The original addresses this table absolutely as
        // g_pItemDefTable + 0x2b20, and 138 * 80 = 11040 = 0x2b20 puts it immediately after the
        // records. Seeking says exactly that, and cannot drift if a field read ever over-reads.
        // A file that stops after the records (an override) simply yields no prices.
        var spellPrices = new List<int>(SpellPriceCount);
        if (resourceStream.CanSeek) {
            resourceStream.Seek((long)RecordCount * RecordSize, SeekOrigin.Begin);
            for (int i = 0; i < SpellPriceCount && resourceStream.Position + sizeof(short) <= resourceStream.Length; i++) {
                spellPrices.Add(reader.ReadInt16());
            }
        }

        return new ObjectInfoSet(id, items, spellPrices);
    }
}
