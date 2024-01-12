namespace ResourceExtractor.Extractors;

using ResourceExtractor.Extensions;

using System.Text;

internal class KeywordExtractor : ExtractorBase {
    public static IEnumerable<string> Extract(string filePath) {
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));

        int size = resourceReader.ReadInt16();
        int amount = resourceReader.ReadInt16();

        int[] offsets = new int[amount];
        for (int i = 0; i < amount; i++) {
            offsets[i] = resourceReader.ReadInt16();
            Console.WriteLine($"{offsets[i]:X4}");
        }
        string[] keywords = new string[amount];
        for (int i = 0; i < amount; i++) {
            resourceReader.BaseStream.Seek(offsets[i], SeekOrigin.Begin);
            keywords[i] = resourceReader.ReadZeroTerminatedString();
        }
        return keywords.ToList();
    }
}