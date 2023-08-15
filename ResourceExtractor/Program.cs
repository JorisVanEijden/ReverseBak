using System.Text;

namespace ResourceExtractor;

using ResourceExtractor.Compression;

internal static class Program {
    private const int FileNameLength = 13;
    private const int TagLength = 4;
    private const int DosCodePage = 437;

    public static void Main(string[] args) {
        CodePagesEncodingProvider.Instance.GetEncoding(DosCodePage);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        string filePath = args.Length == 1
            ? args[0]
            : @"C:\Games\Betrayal at Krondor"; //Directory.GetCurrentDirectory();

        // ExtractResourceArchive(filePath);
        // ExtractFont(Path.Combine(filePath, "book.fnt"));
        Screen screen = ExtractScreen(Path.Combine(filePath, "Z01L.SCX"));
        // File.WriteAllBytes("test.scx", screen.BitMapData);
    }

    private static Screen ExtractScreen(string filePath) {
        var screen = new Screen(filePath);
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));
        ushort signature = resourceReader.ReadUInt16();
        if (signature != 0x27B6) {
            throw new InvalidOperationException($"Invalid SCX signature '{signature}'");
        }
        screen.BitMapData = DecompressToByteArray(resourceReader);

        return screen;
    }

    private static byte[] DecompressToByteArray(BinaryReader resourceReader) {
        byte compressionType = resourceReader.ReadByte();
        int decompressedSize = (int)resourceReader.ReadUInt32();

        ICompression compression = CompressionFactory.Create(compressionType);
        Stream decompressedStream = compression.Decompress(resourceReader.BaseStream);

        byte[] decompressedDataBuffer = new byte[decompressedSize];
        decompressedStream.ReadExactly(decompressedDataBuffer);

        return decompressedDataBuffer;
    }

    private static void ExtractFont(string filePath) {
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));
        string tag = new(resourceReader.ReadChars(TagLength));
        if (!tag.Equals("FNT:")) {
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
        byte compressionType = resourceReader.ReadByte();
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

    private static void ExtractResourceArchive(string filePath) {
        const string resourceFileName = "KRONDOR.001";

        string resourceFilePath = Path.Combine(filePath, resourceFileName);
        using FileStream resourceFile = File.OpenRead(resourceFilePath);
        long resourceFileLength = resourceFile.Length;
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));

        while (resourceFile.Position < resourceFileLength) {
            string fileName = new(resourceReader.ReadChars(FileNameLength));
            fileName = fileName.Trim('\0');
            uint fileSize = resourceReader.ReadUInt32();
            byte[] buffer = resourceReader.ReadBytes((int)fileSize);

            string path = Path.Combine(filePath, fileName);
            Console.WriteLine($"Writing `{fileName}` with a length of {fileSize} bytes.");
            File.WriteAllBytes(path, buffer);
        }
        Console.WriteLine("Done.");
    }
}

// bmx => https://github.com/guidoj/xbak/blob/main/src/imageresource.cc#L88