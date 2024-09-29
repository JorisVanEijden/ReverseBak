namespace ResourceExtractor.Extractors;

using System.Text;

public class OvlExtractor : ExtractorBase {
    public static void Extract(string filePath) {
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));
        string mainTag = ReadTag(resourceReader);
        uint fileSize = resourceReader.ReadUInt16();
        ushort unknown = resourceReader.ReadUInt16();
        while (resourceReader.BaseStream.Position < fileSize) {
            string tag = ReadTag(resourceReader);
            uint chunkSize = resourceReader.ReadUInt32();
            Log($"Reading `{mainTag}` `{tag}` with a length of {chunkSize} bytes.");
            byte[] ovlData = DecompressToByteArray(resourceReader, chunkSize);
            File.WriteAllBytes($"{tag.TrimEnd(':')}.{mainTag}", ovlData);
        }
    }
}