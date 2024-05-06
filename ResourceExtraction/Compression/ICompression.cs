namespace ResourceExtraction.Compression;

using System.IO;

public interface ICompression {
    Stream Compress(Stream inputStream);
    Stream Decompress(Stream inputStream, long length = 0);
}