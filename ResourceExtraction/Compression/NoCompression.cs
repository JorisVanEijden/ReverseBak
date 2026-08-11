namespace ResourceExtraction.Compression;

using ResourceExtraction.Extensions;
using System.IO;

internal class NoCompression : ICompression {
    public Stream Compress(Stream inputStream) {
        return inputStream;
    }

    public Stream Decompress(Stream inputStream, long length = 0) {
        var buffer = new byte[length];
        inputStream.ReadExactly(buffer);

        return new MemoryStream(buffer);
    }
}