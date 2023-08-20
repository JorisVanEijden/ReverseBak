using System.Text;

namespace ResourceExtractor;

using ResourceExtractor.Compression;

using System.Drawing;
using System.Drawing.Imaging;

internal static class Program {
    private const bool Debug = false;

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
        // Screen screen = ExtractScreen(Path.Combine(filePath, "Z01L.SCX"));
        // ExtractOvl(Path.Combine(filePath, "SX.OVL"));
        ExtractAllScx(filePath);
        // ExtractAllBmx(filePath);
    }

    private static void ExtractAllBmx(string bmxFilePath) {
        string[] bmxFiles = Directory.GetFileSystemEntries(bmxFilePath, "*.bmx", new EnumerationOptions {
            MatchCasing = MatchCasing.CaseInsensitive
        });

        Directory.CreateDirectory("BMX");
        foreach (string bmxFile in bmxFiles) {
            BmImage[] bmxImages = ExtractBmx(Path.Combine(bmxFilePath, bmxFile));
            string imageName = $"{Path.GetFileNameWithoutExtension(bmxFile)}";
            Directory.CreateDirectory(Path.Combine("BMX", imageName));
            for (int imageIndex = 0; imageIndex < bmxImages.Length; imageIndex++) {
                BmImage bmxImage = bmxImages[imageIndex];
                string imagePath = Path.Combine("BMX", imageName, $"{imageIndex}.png");
                SaveAsBitmap(bmxImage, imagePath);
            }
        }
    }

    private static void ExtractAllScx(string scxFilePath) {
        string[] scxFiles = Directory.GetFileSystemEntries(scxFilePath, "*.scx", new EnumerationOptions {
            MatchCasing = MatchCasing.CaseInsensitive
        });

        Directory.CreateDirectory("SCX");
        foreach (string scxFile in scxFiles) {
            var screen = ExtractScreen(Path.Combine(scxFilePath, scxFile));
            if (screen.BitMapData == null) {
                continue;
            }
            string imageName = $"{Path.GetFileNameWithoutExtension(scxFile)}";
            string imagePath = Path.Combine("SCX", $"{imageName}.png");
            var image = new BmImage {
                Data = screen.BitMapData,
                Width = 320,
                Height = 200
            };
            SaveAsBitmap(image, imagePath);
        }
    }

    private static void SaveAsBitmap(BmImage image, string imageFileName) {
        if ((image.Flags & 0x20) != 0) {
            var bitmap = new Bitmap(image.Width, image.Height);
            int index = 0;
            for (int x = 0; x < image.Width; x++) {
                for (int y = 0; y < image.Height; y++) {
                    byte colorIndex = image.Data[index++];
                    bitmap.SetPixel(x, y, Color.FromArgb(colorIndex, colorIndex, colorIndex));
                }
            }
            bitmap.Save(imageFileName, ImageFormat.Png);
        } else {
            var bitmap = new Bitmap(image.Width, image.Height);
            int index = 0;
            for (int y = 0; y < image.Height; y++) {
                for (int x = 0; x < image.Width; x++) {
                    byte colorIndex = image.Data[index++];
                    bitmap.SetPixel(x, y, Color.FromArgb(colorIndex, colorIndex, colorIndex));
                }
            }
            bitmap.Save(imageFileName, ImageFormat.Png);
        }
    }

    private static BmImage[] ExtractBmx(string filePath) {
        ICompression rleCompression = CompressionFactory.Create(CompressionType.Rle);
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));
        ushort signature = resourceReader.ReadUInt16();
        if (signature != 0x1066) {
            Console.WriteLine($"Invalid BMX signature '{signature:X4}' in file '{filePath}'");
            return Array.Empty<BmImage>();
        }
        ushort alsoCompressionType = resourceReader.ReadUInt16();
        ushort nrOfImages = resourceReader.ReadUInt16();
        ushort compressedDataSize = resourceReader.ReadUInt16();
        uint decompressedDataSize = resourceReader.ReadUInt32();
        if (Debug)
            Log($"Signature: {signature:X4}, Compression: 0x{alsoCompressionType:X4}, NrOfImages: {nrOfImages}, CompressedDataSize: {compressedDataSize} bytes, DecompressedDataSize: {decompressedDataSize} bytes, file: {Path.GetFileName(filePath)}");
        var images = new BmImage[nrOfImages];
        for (int i = 0; i < nrOfImages; i++) {
            images[i] = new BmImage {
                Size = resourceReader.ReadUInt16(),
                Flags = resourceReader.ReadUInt16(),
                Width = resourceReader.ReadUInt16(),
                Height = resourceReader.ReadUInt16()
            };
        }
        Stream imageStream;
        switch (alsoCompressionType) {
            case 0x0000:
                {
                    Log("Main compression is Lzw");
                    long endPosition = (long)0 == 0 ? resourceReader.BaseStream.Length : resourceReader.BaseStream.Position + 0;
                    var compressionType = (CompressionType)resourceReader.ReadByte();
                    int decompressedSize = (int)resourceReader.ReadUInt32();
                    ICompression compression = CompressionFactory.Create(compressionType);
                    imageStream = compression.Decompress(resourceReader.BaseStream, endPosition);
                    if (imageStream.Length != decompressedSize) {
                        throw new InvalidOperationException($"Decompressed size {imageStream.Length} does not match expected size {decompressedSize}");
                    }
                    break;
                }
            case 0x0001:
                {
                    Log("Main compression is LZSS");
                    ICompression lzssCompression = CompressionFactory.Create(CompressionType.Lzss);
                    imageStream = lzssCompression.Decompress(resourceReader.BaseStream, compressedDataSize);
                    break;
                }
            case 0x0002:
                {
                    Log("Main compression is Rle");
                    imageStream = rleCompression.Decompress(resourceReader.BaseStream, compressedDataSize);
                    break;
                }
            default:
                {
                    Log("Main compression is None");
                    imageStream = resourceReader.BaseStream;
                    break;
                }
        }
        // all data should be read at this point
        for (int i = 0; i < nrOfImages; i++) {
            BmImage image = images[i];
            Log($"Image: {i} size: {image.Size} bytes, flags: {image.Flags:X4}, width: {image.Width}, height: {image.Height}, expectedSize: {image.Height * image.Width} bytes.");
            Log($"Reading image {i} with a size of {image.Size} bytes.");
            if ((image.Flags & 0x80) == 0) {
                Log($"No extra compression for image {i}");
                image.Data = new byte[image.Size];
                imageStream.ReadExactly(image.Data);
            } else {
                Stream decompressedStream = rleCompression.Decompress(imageStream, image.Size);
                image.Data = new byte[decompressedStream.Length];
                decompressedStream.ReadExactly(image.Data);
            }
            if (image.Height * image.Width != image.Data.Length) {
                File.WriteAllBytes("debug.bin", image.Data);
                throw new InvalidOperationException($"Image {i} has a size of {image.Data.Length} bytes but should be {image.Height} * {image.Width} = {image.Height * image.Width} bytes.");
            }
        }
        return images;
    }

    private static void Log(string message) {
        if (Debug)
            Console.WriteLine(message);
    }

    private static void ExtractOvl(string filePath) {
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));
        string mainTag = ReadTag(resourceReader);
        uint fileSize = resourceReader.ReadUInt16();
        ushort unknown = resourceReader.ReadUInt16();
        while (resourceReader.BaseStream.Position < fileSize) {
            string tag = ReadTag(resourceReader);
            uint chunkSize = resourceReader.ReadUInt32();
            Console.WriteLine($"Reading `{mainTag}` `{tag}` with a length of {chunkSize} bytes.");
            byte[] ovlData = DecompressToByteArray(resourceReader, chunkSize);
            File.WriteAllBytes($"{tag.TrimEnd(':')}.{mainTag}", ovlData);
        }
    }

    private static string ReadTag(BinaryReader resourceReader) {
        string tagString = new(resourceReader.ReadChars(TagLength));
        return tagString.TrimEnd(':');
    }

    private static Screen ExtractScreen(string filePath) {
        var screen = new Screen(filePath);
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));
        ushort signature = resourceReader.ReadUInt16();
        if (signature != 0x27B6) {
            if (signature == 0x4D42) {
                resourceFile.Seek(0, SeekOrigin.Begin);
                return ExtractScreenFromTaggedBitmap(resourceReader);
            }
            throw new InvalidOperationException($"Invalid SCX signature '{signature:X4}' in {filePath}");
        }
        screen.BitMapData = DecompressToByteArray(resourceReader);

        return screen;
    }

    private static Screen ExtractScreenFromTaggedBitmap(BinaryReader resourceReader) {
        throw new NotImplementedException();
    }

    private static byte[] DecompressToByteArray(BinaryReader resourceReader, long length = 0) {
        long endPosition = length == 0 ? resourceReader.BaseStream.Length : resourceReader.BaseStream.Position + length;
        CompressionType compressionType = (CompressionType)resourceReader.ReadByte();
        int decompressedSize = (int)resourceReader.ReadUInt32();

        ICompression compression = CompressionFactory.Create(compressionType);
        Stream decompressedStream = compression.Decompress(resourceReader.BaseStream, endPosition);

        byte[] decompressedDataBuffer = new byte[decompressedSize];
        decompressedStream.ReadExactly(decompressedDataBuffer);

        return decompressedDataBuffer;
    }

    private static void ExtractFont(string filePath) {
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

internal class BmImage {
    public ushort Size { get; set; }
    public ushort Flags { get; set; }
    public ushort Width { get; set; }
    public ushort Height { get; set; }
    public byte[]? Data { get; set; }
}

// bmx => https://github.com/guidoj/xbak/blob/main/src/imageresource.cc#L88