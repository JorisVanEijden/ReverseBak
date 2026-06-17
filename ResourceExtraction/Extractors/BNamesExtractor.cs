namespace ResourceExtraction.Extractors;

using GameData.Resources.Creature;
using System.IO;
using System.Text;

/// <summary>Extracts BNAMES.DAT — the per-creature bitmap directory — into the engine-independent
/// <see cref="CreatureBitmaps"/>. Layout: <c>u16 count; u16 offset[count]; u16 byteCount; byte data[byteCount]</c>.
/// Each <c>data[offset]</c> entry is <c>strz name; sbyte numbers[3]; sbyte colorSet</c> (fixed 8-byte window;
/// names are ≤3 chars). A creature's bitmaps are <c>&lt;name&gt;&lt;number&gt;.BMX</c>; the colorSet is baked
/// into the emitted key (<c>_CS&lt;n&gt;</c>) and not otherwise exposed. Negative numbers are runtime aliases
/// to another creature (unused in the shipped data) and are skipped.</summary>
public class BNamesExtractor : ExtractorBase<CreatureBitmaps> {
    public override CreatureBitmaps Extract(string id, Stream resourceStream) {
        var result = new CreatureBitmaps(id);
        using var reader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));

        ushort count = reader.ReadUInt16();
        var offsets = new ushort[count];
        for (int i = 0; i < count; i++) {
            offsets[i] = reader.ReadUInt16();
        }
        ushort byteCount = reader.ReadUInt16();
        byte[] data = reader.ReadBytes(byteCount);

        for (int creatureId = 0; creatureId < count; creatureId++) {
            int o = offsets[creatureId];

            int end = o;
            while (end < data.Length && data[end] != 0) {
                end++;
            }
            string name = Encoding.ASCII.GetString(data, o, end - o);
            if (name.Length == 0) {
                continue; // unused slot (offset points at the empty entry)
            }

            sbyte colorSet = (sbyte)data[o + 7];
            var sprite = new CreatureSprite { CreatureId = creatureId, Name = name };
            for (int b = 0; b < 3; b++) {
                sbyte number = (sbyte)data[o + 4 + b];
                if (number < 0) {
                    continue; // runtime alias to another creature's bitmap; no asset of its own
                }
                string stem = $"{name.ToUpperInvariant()}{number}";
                sprite.SpriteKeys.Add(colorSet >= 0 ? $"{stem}_CS{colorSet}" : stem);
            }
            result.Creatures.Add(sprite);
        }

        return result;
    }
}
