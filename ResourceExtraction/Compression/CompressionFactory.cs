namespace ResourceExtraction.Compression;

using ResourceExtractor.Compression;

using System;

public static class CompressionFactory {
    public static ICompression Create(CompressionType compressionType) {
        return compressionType switch {
            CompressionType.None => new NoCompression(),
            CompressionType.Rle => new RleCompression(),
            CompressionType.Lzw => new LzwCompression(),
            CompressionType.Lzss => new LzssCompression(),
            _ => throw new InvalidOperationException($"Invalid compression type '{compressionType}'")
        };
    }
}