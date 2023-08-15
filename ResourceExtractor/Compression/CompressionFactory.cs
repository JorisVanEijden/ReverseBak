namespace ResourceExtractor.Compression;

internal static class CompressionFactory {
    public static ICompression Create(byte compressionType) {
        return compressionType switch {
            0 => new NoCompression(),
            1 => new RleCompression(),
            2 => new LzwCompression(),
            3 => throw new NotImplementedException("new LzhCompression()"), 
            _ => throw new InvalidOperationException($"Invalid compression type '{compressionType}'")
        };
    }
}