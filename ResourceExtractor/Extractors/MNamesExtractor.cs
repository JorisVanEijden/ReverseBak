namespace ResourceExtractor.Extractors;

using ResourceExtractor.Extensions;

using System.Text;

internal class MNamesExtractor : ExtractorBase {
    public static IEnumerable<string> Extract(string filePath) {
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));

        int amount = resourceReader.ReadInt16();

        int[] offsets = new int[amount];
        for (int i = 0; i < amount; i++) {
            offsets[i] = resourceReader.ReadInt16();
        }
        int size = resourceReader.ReadInt16();
        long offset = resourceReader.BaseStream.Position;

        string[] mNames = new string[amount];
        for (int i = 0; i < amount; i++) {
            resourceReader.BaseStream.Seek(offsets[i] + offset, SeekOrigin.Begin);
            mNames[i] = $"{resourceReader.ReadZeroTerminatedString()} = {i},";
        }
        return mNames.ToList();
    }
}