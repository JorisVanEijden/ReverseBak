namespace ResourceExtractor.Compression; 

public interface ICompression {
    Stream Compress(Stream inputStream);
    Stream Decompress(Stream inputStream);
}