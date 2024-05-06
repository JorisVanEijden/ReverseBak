namespace ResourceExtraction.Compression;

using System.IO;

internal class NoCompression : ICompression {
    public Stream Compress(Stream inputStream) {
        return inputStream;
    }

    public Stream Decompress(Stream inputStream, long length = 0) {
        return inputStream;
    }
}