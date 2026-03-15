namespace ResourceExtractor.Extractors;

using GameData.Resources.Image;

using ResourceExtraction.Compression;

using ResourceExtractor.Compression;

using System.Drawing;
using System.Drawing.Imaging;

public abstract class ExtractorBase {
    internal const int FileNameLength = 13;
    private const int TagLength = 4;
    internal const int DosCodePage = 437;
    internal const bool Debug = false;
    protected static string Indent = string.Empty;

    protected static void Log(string message) {
        if (Debug)
            Console.WriteLine(Indent + message);
    }

   
    protected static byte[] DecompressToByteArray(BinaryReader resourceReader, long length = 0) {
        long endPosition = length == 0 ? resourceReader.BaseStream.Length : resourceReader.BaseStream.Position + length;
        var compressionType = (CompressionType)resourceReader.ReadByte();
        var decompressedSize = (int)resourceReader.ReadUInt32();

        ICompression compression = CompressionFactory.Create(compressionType);
        Stream decompressedStream = compression.Decompress(resourceReader.BaseStream, endPosition);

        var decompressedDataBuffer = new byte[decompressedSize];
        decompressedStream.ReadExactly(decompressedDataBuffer);

        return decompressedDataBuffer;
    }
}