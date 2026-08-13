namespace ResourceExtraction.Extractors;

using GameData.Resources.Data;
using System.IO;

/// <summary>
/// <c>OBJFIXED.DAT</c> — the shipped fixed-object placements.
///
/// <para>Lives here rather than in the extractor CLI because the game needs it <b>at runtime</b>:
/// it is the second source <c>actorspawn_objfixed</c> consults, and it carries the per-placement
/// dialog id and lock that doors, ladders and containers are driven by.</para>
///
/// <para>The file is a sequence of count-prefixed blocks whose records are byte-for-byte the ones
/// the save holds, so parsing reuses <c>SaveGameExtractor.ParseContainer</c>.</para>
/// </summary>
public class ObjFixedExtractor : ExtractorBase<FixedObjectSet> {
    public override FixedObjectSet Extract(string id, Stream resourceStream) {
        // No text in these records, so no DOS code page needed — which also keeps this usable
        // wherever the encoding provider has not been registered.
        using var reader = new BinaryReader(resourceStream);
        var set = new FixedObjectSet(id);
        while (reader.BaseStream.Position < reader.BaseStream.Length - 1) {
            ushort count = reader.ReadUInt16();
            for (var i = 0; i < count; i++) {
                set.Containers.Add(SaveGameExtractor.ParseContainer(reader));
            }
        }
        return set;
    }
}
