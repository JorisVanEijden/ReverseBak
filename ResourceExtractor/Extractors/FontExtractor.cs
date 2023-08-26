namespace ResourceExtractor.Extractors;

using ResourceExtractor.Compression;

using System.Text;

public class FontExtractor : ExtractorBase {
    public static void Extract(string filePath) {
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));
        string tag = ReadTag(resourceReader);
        if (!tag.Equals("FNT")) {
            throw new InvalidOperationException($"Invalid tag '{tag}'");
        }
        uint fileSize = resourceReader.ReadUInt32();
        var unknown1 = resourceReader.ReadByte();
        var unknown2 = resourceReader.ReadByte();
        byte fontHeight = resourceReader.ReadByte();
        var unknown3 = resourceReader.ReadByte();
        var firstCharacter = resourceReader.ReadByte();
        byte nrOfChars = resourceReader.ReadByte();
        var dataLength = resourceReader.ReadUInt16();
        CompressionType compressionType = (CompressionType)resourceReader.ReadByte();
        uint decompressedSize = resourceReader.ReadUInt32();
        ICompression compression = CompressionFactory.Create(compressionType);
        Stream decompressedStream = compression.Decompress(resourceReader.BaseStream);
        using var resultReader = new BinaryReader(decompressedStream);
        // offsets
        var offsets = new int[nrOfChars];
        for (int i = 0; i < nrOfChars; i++) {
            var characterOffset = resultReader.ReadUInt16();
            offsets[i] = characterOffset;
            Console.WriteLine($"{i}: {characterOffset}");
        }
        // widths
        var widths = new int[nrOfChars];
        for (int i = 0; i < nrOfChars; i++) {
            var characterWidth = resultReader.ReadByte();
            widths[i] = characterWidth;
            Console.WriteLine($"{i}: {characterWidth}");
        }
        // data
        for (int i = 0; i < nrOfChars; i++) {
            var characterWidth = widths[i];
            Console.WriteLine($"{i}: ");
            for (int j = 0; j < fontHeight; j++) {
                var data = resultReader.ReadByte() << 8;
                if (characterWidth > 8) {
                    data |= resultReader.ReadByte();
                }
                for (int k = 0; k < characterWidth; k++) {
                    Console.Write((data & 1 << (16 - k)) != 0 ? "##" : "  ");
                }
                Console.Write("\n");
            }
        }

        Console.WriteLine("Done.");
    }
}