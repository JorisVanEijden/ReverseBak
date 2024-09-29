namespace ResourceExtractor.Extractors;

using ResourceExtraction.Compression;

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
        byte version = resourceReader.ReadByte();
        byte fontWidth = resourceReader.ReadByte();
        byte fontHeight = resourceReader.ReadByte();
        byte baseline = resourceReader.ReadByte();
        byte firstCharacter = resourceReader.ReadByte();
        byte nrOfChars = resourceReader.ReadByte();
        ushort dataLength = resourceReader.ReadUInt16();
        var compressionType = (CompressionType)resourceReader.ReadByte();
        uint decompressedSize = resourceReader.ReadUInt32();
        ICompression compression = CompressionFactory.Create(compressionType);
        Stream decompressedStream = compression.Decompress(resourceReader.BaseStream);
        using var resultReader = new BinaryReader(decompressedStream);
        // offsets
        int[] offsets = new int[nrOfChars];
        for (int i = 0; i < nrOfChars; i++) {
            ushort characterOffset = resultReader.ReadUInt16();
            offsets[i] = characterOffset;
            Log($"{i}: {characterOffset}");
        }
        // widths
        int[] widths = new int[nrOfChars];
        for (int i = 0; i < nrOfChars; i++) {
            byte characterWidth = resultReader.ReadByte();
            widths[i] = characterWidth;
            Log($"{i}: {characterWidth}");
        }
        // data
        for (int i = 0; i < nrOfChars; i++) {
            int characterWidth = widths[i];
            Log($"{i}: ");
            for (int j = 0; j < fontHeight; j++) {
                int data = resultReader.ReadByte() << 8;
                if (characterWidth > 8) {
                    data |= resultReader.ReadByte();
                }
                for (int k = 0; k < characterWidth; k++) {
                    Console.Write((data & 1 << 16 - k) != 0 ? "##" : "  ");
                }
                Console.Write("\n");
            }
        }

        Log("Done.");
    }
}