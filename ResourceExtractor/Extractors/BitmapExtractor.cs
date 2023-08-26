namespace ResourceExtractor.Extractors;

using ResourceExtractor.Compression;
using ResourceExtractor.Resources;

using System.Text;

public class BitmapExtractor : ExtractorBase {
    public static BmImage[] Extract(string filePath) {
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
                    long endPosition = resourceReader.BaseStream.Length;
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

    private static void ExtractAllBmx(string bmxFilePath) {
        const string resourceDirectory = "BMX";
        if (!Directory.Exists(resourceDirectory)) {
            Directory.CreateDirectory(resourceDirectory);
        }
        string[] bmxFiles = Directory.GetFileSystemEntries(bmxFilePath, "*.bmx", new EnumerationOptions {
            MatchCasing = MatchCasing.CaseInsensitive
        });

        foreach (string bmxFile in bmxFiles) {
            BmImage[] bmxImages = Extract(Path.Combine(bmxFilePath, bmxFile));
            string imageName = $"{Path.GetFileNameWithoutExtension(bmxFile)}";
            Directory.CreateDirectory(Path.Combine(resourceDirectory, imageName));
            for (int imageIndex = 0; imageIndex < bmxImages.Length; imageIndex++) {
                BmImage bmxImage = bmxImages[imageIndex];
                string imagePath = Path.Combine(resourceDirectory, imageName, $"{imageIndex}.png");
                SaveAsBitmap(bmxImage, imagePath);
            }
        }
    }
}