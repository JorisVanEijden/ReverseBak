namespace ResourceExtraction.Extractors;

using GameData.Resources;

using ResourceExtraction.Compression;
using ResourceExtraction.Extensions;

using ResourceExtractor.Compression;

using System;
using System.IO;

public abstract class ExtractorBase<T> where T : IResource {
    private const int TagLength = 4;
    internal const int DosCodePage = 437;
    internal const bool Debug = false;
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
        var decompressedSize = (int)resourceReader.ReadUInt32();

        ICompression compression = CompressionFactory.Create(compressionType);
        Stream decompressedStream = compression.Decompress(resourceReader.BaseStream, endPosition);

        var decompressedDataBuffer = new byte[decompressedSize];
        decompressedStream.ReadExactly(decompressedDataBuffer);

        return decompressedDataBuffer;
    }

    public abstract T Extract(string id, Stream resourceStream);
}