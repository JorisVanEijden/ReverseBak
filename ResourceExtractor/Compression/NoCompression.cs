namespace ResourceExtractor.Compression;

internal class NoCompression : ICompression {
    public Stream Compress(Stream inputStream) {
        return inputStream;
    }

    public Stream Decompress(Stream inputStream) {
        return inputStream;
    }
}