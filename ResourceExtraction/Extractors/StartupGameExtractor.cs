namespace ResourceExtraction.Extractors;

using GameData.Resources.Data;

using System.IO;
using System.Text;

public class StartupGameExtractor : ExtractorBase<StartupGame> {
    public override StartupGame Extract(string id, Stream resourceStream) {
        using var resourceReader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        // Read the remainder of the stream as raw bytes. If the format is later reverse-engineered,
        // this extractor can be expanded to parse into strongly-typed fields.
        long remaining = resourceReader.BaseStream.Length - resourceReader.BaseStream.Position;
        byte[] data = resourceReader.ReadBytes((int)remaining);
        return new StartupGame(id, data);
    }
}
