namespace ResourceExtraction.Extractors;

using GameData.Resources.Object;
using ResourceExtraction.Extensions;
using System.IO;
using System.Text;

/// <summary>
/// Parses ONAMES.DAT — the indexed object-name table. See <see cref="ObjectNames"/>
/// for the format and how it differs from the names in OBJINFO.DAT.
///
/// Layout: u16 count; (count+1) u16 offsets relative to the string base
/// (= 2 + 2*(count+1)); NUL-terminated strings. The final offset is an end
/// sentinel and is not a name.
/// </summary>
public class OnamesExtractor : ExtractorBase<ObjectNames> {
    public override ObjectNames Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        var data = new ObjectNames(id);

        int count = reader.ReadUInt16();
        long stringBase = 2 + 2 * (count + 1L); // offsets are relative to this

        var offsets = new ushort[count];
        for (int i = 0; i < count; i++) {
            offsets[i] = reader.ReadUInt16();
        }

        for (int i = 0; i < count; i++) {
            reader.BaseStream.Seek(stringBase + offsets[i], SeekOrigin.Begin);
            data.Names.Add(reader.ReadZeroTerminatedString());
        }

        return data;
    }
}
