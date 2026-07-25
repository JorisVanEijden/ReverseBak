namespace ResourceExtraction.Extractors;

using GameData.Resources.Content;
using GameData.Resources.Creature;
using ResourceExtraction.Extensions;
using System.IO;
using System.Text;

/// <summary>Parses MNAMES.DAT — the creature-name table (the <c>mnames</c> id space). Layout:
/// <c>u16 count</c>, <c>count × u16 offset</c> (blob-relative), <c>u16 blobSize</c>, then the
/// NUL-terminated string blob. Each entry's stable key is <c>base:mnames:&lt;index&gt;</c>. This is the
/// de-indexed target catalog for encounter <c>EnemySlot.CreatureNumber</c> (reference #15).</summary>
public class MnamesExtractor : ExtractorBase<CreatureNames> {
    public override CreatureNames Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        var data = new CreatureNames(id);

        int count = reader.ReadUInt16();
        var offsets = new int[count];
        for (int i = 0; i < count; i++) {
            offsets[i] = reader.ReadUInt16();
        }
        reader.ReadUInt16(); // blob size (unused; strings run NUL-terminated)
        long blobBase = reader.BaseStream.Position;

        for (int i = 0; i < count; i++) {
            reader.BaseStream.Seek(blobBase + offsets[i], SeekOrigin.Begin);
            data.Creatures.Add(new CreatureName {
                Number = i,
                Key = ContentKey.ForBase("mnames", i),
                Name = reader.ReadZeroTerminatedString(),
            });
        }
        return data;
    }
}
