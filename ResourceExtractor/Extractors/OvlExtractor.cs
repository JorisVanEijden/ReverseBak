namespace ResourceExtractor.Extractors;

using System.Text;

public class OvlExtractor : ExtractorBase {
    public static void Extract(string filePath, string filename) {
        using var resourceFile = File.OpenRead(Path.Join(filePath, filename));
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));
        string mainTag = ReadTag(resourceReader);
        uint fileSize = resourceReader.ReadUInt16();
        ushort unknown = resourceReader.ReadUInt16();
        var dir = Directory.CreateDirectory(Path.Join(filePath, mainTag));
        while (resourceReader.BaseStream.Position < fileSize) {
            string tag = ReadTag(resourceReader);
            uint chunkSize = resourceReader.ReadUInt32();
            Log($"Reading `{mainTag}` `{tag}` with a length of {chunkSize} bytes.");
            byte[] ovlData = DecompressToByteArray(resourceReader, chunkSize);
            File.WriteAllBytes(Path.Join(dir.FullName, $"{tag.TrimEnd(':')}.bin"), ovlData);
        }
    }
}