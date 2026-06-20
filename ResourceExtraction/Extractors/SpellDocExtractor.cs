namespace ResourceExtraction.Extractors;

using GameData.Resources.Spells;
using ResourceExtraction.Extensions;
using System.IO;
using System.Text;

/// <summary>
/// Parses SPELLDOC.DAT — the indexed spell-description string table: u16 count, count × u32
/// blob offsets, a u16 declared size (= file size in shipped data), then a NUL-terminated
/// string blob running to EOF. See <see cref="SpellDescriptions"/> for the semantics and
/// the IDA references (Load_spells @ ovr173:0x66700).
/// </summary>
public class SpellDocExtractor : ExtractorBase<SpellDescriptions> {
    public override SpellDescriptions Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        var data = new SpellDescriptions(id);

        int count = reader.ReadUInt16();
        var offsets = new uint[count];
        for (int i = 0; i < count; i++) {
            offsets[i] = reader.ReadUInt32();
        }
        _ = reader.ReadUInt16();              // declared size (= whole file size; blob is to EOF)
        long blobStart = reader.BaseStream.Position;
        long blobLength = reader.BaseStream.Length - blobStart;

        foreach (uint offset in offsets) {
            if (offset >= blobLength) {
                data.Descriptions.Add("");
                continue;
            }
            reader.BaseStream.Seek(blobStart + offset, SeekOrigin.Begin);
            data.Descriptions.Add(reader.ReadZeroTerminatedString());
        }

        return data;
    }
}
