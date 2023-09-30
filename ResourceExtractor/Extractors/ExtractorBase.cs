namespace ResourceExtractor.Extractors;

using ResourceExtractor.Compression;
using ResourceExtractor.Resources;
using ResourceExtractor.Resources.Image;

using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

public abstract class ExtractorBase {
    internal const int FileNameLength = 13;
    private const int TagLength = 4;
    internal const int DosCodePage = 437;
    internal const bool Debug = false; // true;
    protected static string Indent = string.Empty;

    protected static string ReadTag(BinaryReader resourceReader) {
        string tagString = new(resourceReader.ReadChars(TagLength));
        return tagString.TrimEnd(':');
    }

    protected static void Log(string message) {
        if (Debug)
            Console.WriteLine(Indent + message);
    }

    protected static byte[] DecompressToByteArray(BinaryReader resourceReader, long length = 0) {
        long endPosition = length == 0 ? resourceReader.BaseStream.Length : resourceReader.BaseStream.Position + length;
        var compressionType = (CompressionType)resourceReader.ReadByte();
        int decompressedSize = (int)resourceReader.ReadUInt32();

        ICompression compression = CompressionFactory.Create(compressionType);
        Stream decompressedStream = compression.Decompress(resourceReader.BaseStream, endPosition);

        byte[] decompressedDataBuffer = new byte[decompressedSize];
        decompressedStream.ReadExactly(decompressedDataBuffer);

        return decompressedDataBuffer;
    }

    protected static void SaveAsBitmap(BmImage image, string imageFileName, Color[]? palette = null) {
        if ((image.Flags & 0x20) != 0 && image.Data != null) {
            var bitmap = new Bitmap(image.Width, image.Height);
            int index = 0;
            for (int x = 0; x < image.Width; x++) {
                for (int y = 0; y < image.Height; y++) {
                    byte colorIndex = image.Data[index++];
                    if (palette != null) {
                        bitmap.SetPixel(x, y, palette[colorIndex]);
                    } else {
                        bitmap.SetPixel(x, y, Color.FromArgb(colorIndex, colorIndex, colorIndex));
                    }
                }
            }
            bitmap.Save(imageFileName, ImageFormat.Png);
        } else {
            var bitmap = new Bitmap(image.Width, image.Height);
            int index = 0;
            for (int y = 0; y < image.Height; y++) {
                for (int x = 0; x < image.Width; x++) {
                    byte colorIndex = image.Data[index++];
                    if (palette != null) {
                        bitmap.SetPixel(x, y, palette[colorIndex]);
                    } else {
                        bitmap.SetPixel(x, y, Color.FromArgb(colorIndex, colorIndex, colorIndex));
                    }
                }
            }
            bitmap.Save(imageFileName, ImageFormat.Png);
        }
    }

    protected static string ReadString(BinaryReader resourceReader) {
        var text = new StringBuilder();
        char character = resourceReader.ReadChar();
        while (character != '\0') {
            text.Append(character);
            character = resourceReader.ReadChar();
        }

        return text.ToString();
    }
}