namespace ResourceExtraction.Extractors;

using GameData.Resources.Image;

using ResourceExtraction.Compression;
using ResourceExtraction.Extensions;
using ResourceExtraction.Imaging;

using ResourceExtractor.Compression;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class BitmapExtractor : ExtractorBase<ImageSet> {
    private const double NormalScreenWidth = 320.0;
    private const double NormalScreenHeight = 200.0;

    public override ImageSet Extract(string id, Stream resourceStream) {
        var imageSet = new ImageSet(id);
        using var resourceReader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        ushort signature = resourceReader.ReadUInt16();
        switch (signature) {
            case 0x1066:
                imageSet.Images = ReadNormalBmxStream(id, resourceReader);

                break;
            case 0x4D42:
                resourceStream.Seek(0, SeekOrigin.Begin);

                imageSet.Images = ReadTaggedBmxStream(id, resourceReader);

                break;
            default:
                throw new InvalidOperationException($"Invalid BMX signature '{signature:X4}' in file '{id}'");
        }

        return imageSet;
    }

    private static List<BmImage> ReadTaggedBmxStream(string name, BinaryReader resourceReader) {
        string mainTag = resourceReader.ReadTag();
        if (!mainTag.Equals("BMP")) {
            throw new InvalidOperationException($"Invalid tag '{mainTag}', expected 'BMP'");
        }
        ushort fileSize = resourceReader.ReadUInt16();
        ushort unknown = resourceReader.ReadUInt16();
        string infTag = resourceReader.ReadTag();
        if (!infTag.Equals("INF")) {
            throw new InvalidOperationException($"Invalid tag '{infTag}', expected 'INF'");
        }
        uint infDataSize = resourceReader.ReadUInt32();
        ushort nrOfImages = resourceReader.ReadUInt16();
        Log($"File: {name}, NrOfImages: {nrOfImages}");
        var images = new BmImage[nrOfImages];
        for (var i = 0; i < nrOfImages; i++) {
            images[i] = new BmImage($"{name}[{i}]") {
                Width = resourceReader.ReadUInt16()
            };
        }
        for (var i = 0; i < nrOfImages; i++) {
            images[i].Height = resourceReader.ReadUInt16();
        }
        for (var i = 0; i < nrOfImages; i++) {
            images[i].ScaleX = images[i].Width / NormalScreenWidth;
            images[i].ScaleY = images[i].Height / NormalScreenHeight;
        }
        string binTag = resourceReader.ReadTag();
        if (!binTag.Equals("BIN")) {
            throw new InvalidOperationException($"Invalid tag '{binTag}', expected 'BIN'");
        }
        uint binDataSize = resourceReader.ReadUInt32();
        var compressionType = (CompressionType)resourceReader.ReadByte();
        uint decompressedSize = resourceReader.ReadUInt32();
        Log($"File: {name}, Compression: {compressionType}, DecompressedDataSize: {decompressedSize} bytes");
        ICompression compression = CompressionFactory.Create(compressionType);
        Stream decompressedStream = compression.Decompress(resourceReader.BaseStream);

        if (decompressedStream.Length != decompressedSize) {
            throw new InvalidOperationException($"Decompressed size {decompressedStream.Length} does not match expected size {decompressedSize}");
        }

        for (var i = 0; i < nrOfImages; i++) {
            BmImage image = images[i];
            image.BitMapData = new byte[image.Width * image.Height];
            Log($"Reading image {i} with a size of {image.Width * image.Height} bytes at position {decompressedStream.Position}.");

            for (var j = 0; j < image.BitMapData.Length; j++) {
                int colors = decompressedStream.ReadByte();
                image.BitMapData[j++] = (byte)(colors >> 4);
                image.BitMapData[j] = (byte)(colors & 0x0F);
            }
        }

        // Tagged BMX images are stored row-major; correct to square pixels.
        ApplyAspectCorrection(images, name, columnMajorPerImage: false);

        return images.ToList();
    }

    private static List<BmImage> ReadNormalBmxStream(string name, BinaryReader resourceReader) {
        ICompression rleCompression = CompressionFactory.Create(CompressionType.Rle);
        ushort alsoCompressionType = resourceReader.ReadUInt16();
        ushort nrOfImages = resourceReader.ReadUInt16();
        ushort compressedDataSize = resourceReader.ReadUInt16();
        uint decompressedDataSize = resourceReader.ReadUInt32();
        Log($"File: {name}, Compression: 0x{alsoCompressionType:X4}, NrOfImages: {nrOfImages}, CompressedDataSize: {compressedDataSize} bytes, DecompressedDataSize: {decompressedDataSize} bytes");
        var images = new BmImage[nrOfImages];
        for (var i = 0; i < nrOfImages; i++) {
            images[i] = new BmImage($"{name}[{i}]") {
                Size = resourceReader.ReadUInt16(),
                Flags = (ImageFlags)resourceReader.ReadUInt16(),
                Width = resourceReader.ReadUInt16(),
                Height = resourceReader.ReadUInt16()
            };
            images[i].ScaleX = images[i].Width / NormalScreenWidth;
            images[i].ScaleY = images[i].Height / NormalScreenHeight;
        }
        Stream imageStream;
        switch (alsoCompressionType) {
            case 0x0000:
                {
                    Log("Main compression is Lzw");
                    long endPosition = resourceReader.BaseStream.Length;
                    var compressionType = (CompressionType)resourceReader.ReadByte();
                    var decompressedSize = (int)resourceReader.ReadUInt32();
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
        for (var i = 0; i < nrOfImages; i++) {
            BmImage image = images[i];
            Log($"Image: {i} size: {image.Size} bytes, flags: {(short)image.Flags:X4}, width: {image.Width}, height: {image.Height}, expectedSize: {image.Height * image.Width} bytes.");

            if (!image.Flags.HasFlag(ImageFlags.Compressed)) {
                Log($"No extra compression for image {i}");
                image.BitMapData = new byte[image.Size];
                imageStream.ReadExactly(image.BitMapData);
            } else {
                Stream decompressedStream = rleCompression.Decompress(imageStream, image.Size);
                image.BitMapData = new byte[decompressedStream.Length];
                decompressedStream.ReadExactly(image.BitMapData);
            }
            if (image.Height * image.Width != image.BitMapData.Length) {
                File.WriteAllBytes("debug.bin", image.BitMapData);

                throw new InvalidOperationException($"Image {i} has a size of {image.BitMapData.Length} bytes but should be {image.Height} * {image.Width} = {image.Height * image.Width} bytes.");
            }
        }

        // Normal BMX images may be stored column-major (ReversedRowColumn); correct to square pixels.
        ApplyAspectCorrection(images, name, columnMajorPerImage: true);

        return images.ToList();
    }

    /// <summary>
    /// Converts each image's non-square source pixels to square pixels. Book images (BOOK.BMX) live in
    /// 640x350 EGA space and use the EGA correction so they compose at the same scale as BOOK.SCX; all
    /// other BMX images live in 320x200 VGA space.
    /// </summary>
    private static void ApplyAspectCorrection(IReadOnlyList<BmImage> images, string name, bool columnMajorPerImage) {
        bool isBook = name.StartsWith("BOOK", StringComparison.OrdinalIgnoreCase);
        foreach (BmImage image in images) {
            bool columnMajor = columnMajorPerImage && image.Flags.HasFlag(ImageFlags.ReversedRowColumn);
            image.BitMapData = isBook
                ? AspectCorrection.CorrectEga(image.BitMapData!, image.Width, image.Height, columnMajor, out int w, out int h)
                : AspectCorrection.CorrectVga(image.BitMapData!, image.Width, image.Height, columnMajor, out w, out h);
            image.Width = w;
            image.Height = h;
        }
    }

    public BmImage ExtractSingle(string id, Stream resourceStream) {
        (string name, int index) = GetNameAndIndex(id);

        using var resourceReader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        ushort signature = resourceReader.ReadUInt16();
        switch (signature) {
            case 0x1066:
                return ReadNormalBmxStream(name, resourceReader)[index];
            case 0x4D42:
                resourceStream.Seek(0, SeekOrigin.Begin);

                return ReadTaggedBmxStream(name, resourceReader)[index];
            default:
                throw new InvalidOperationException($"Invalid BMX signature '{signature:X4}' in file '{id}'");
        }
    }

    private static (string, int) GetNameAndIndex(string key) {
        int i = key.IndexOf('[');

        if (i <= 0)
            return (key, 0);
        int j = key.LastIndexOf(']');

        if (j <= i)
            return (key, 0);
        string name = key[..i];
        var index = Convert.ToInt32(key.Substring(i + 1, j - (i + 1)));

        return (name, index);
    }
}