namespace ResourceExtraction.Extractors;

using GameData.Resources.Palette;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class RemapExtractor : ExtractorBase<RemapResource> {
    public override RemapResource Extract(string id, Stream resourceStream) {
        using var resourceReader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        var remap = new RemapResource(id);
        var mappingId = 0;
        while (resourceReader.BaseStream.Position < resourceReader.BaseStream.Length) {
            var mapping = new Dictionary<byte, byte>();
            for (var i = 0; i <= byte.MaxValue; i++) {
                byte index = resourceReader.ReadByte();
                if (index != 0 && index != i) {
                    mapping[(byte)i] = index;
                }
            }
            remap.Mappings[mappingId++] = mapping;
        }

        return remap;
    }
}