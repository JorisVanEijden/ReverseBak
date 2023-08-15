namespace ResourceExtractor.Compression;

internal class RleCompression : ICompression {
    public Stream Compress(Stream inputStream) {
        throw new NotImplementedException();
    }

    public Stream Decompress(Stream inputStream) {
        var outputStream = new MemoryStream();

        while (inputStream.Position < inputStream.Length) {
            int marker = inputStream.ReadByte();
            if (marker > 128) {
                int data = inputStream.ReadByte();
                for (int i = 0; i < marker - 128; i++) {
                    outputStream.WriteByte((byte)data);
                }
            } else {
                for (int i = 0; i < marker; i++) {
                    int data = inputStream.ReadByte();
                    outputStream.WriteByte((byte)data);
                }
            }
        }
        outputStream.Seek(0, SeekOrigin.Begin);
        return outputStream;
    }
}