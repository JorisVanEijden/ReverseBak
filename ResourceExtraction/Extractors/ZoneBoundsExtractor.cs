namespace ResourceExtraction.Extractors;

using GameData.Resources.World;
using System.IO;
using System.Text;

public class ZoneBoundsExtractor : ExtractorBase<ZoneBounds> {
    public override ZoneBounds Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        return new ZoneBounds(id) {
            XOffset = reader.ReadUInt16(),
            YOffset = reader.ReadUInt16(),
            Width = reader.ReadUInt16(),
            Height = reader.ReadUInt16()
        };
    }
}
