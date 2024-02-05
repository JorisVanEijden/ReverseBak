namespace ResourceExtractor.Compression;

internal class RleCompression : ICompression {
    public Stream Compress(Stream inputStream) {
        throw new NotImplementedException();
    }

    public Stream Decompress(Stream inputStream, long length = 0) {
        var outputStream = new MemoryStream();
        long startPosition = inputStream.Position;
        long endPosition = length == 0 ? inputStream.Length : startPosition + length;

        while (inputStream.Position < endPosition) {
            int marker = inputStream.ReadByte();
            switch (marker) {
                case > 0x80:
                    {
                        int data = inputStream.ReadByte();
                        for (int i = 0; i < marker - 0x80; i++) {
                            outputStream.WriteByte((byte)data);
                        }
                        break;
                    }
                case 0:
                    inputStream.Seek(endPosition - inputStream.Position, SeekOrigin.Current);
                    break;
                default:
                    {
                        for (int i = 0; i < marker; i++) {
                            int data = inputStream.ReadByte();
                            outputStream.WriteByte((byte)data);
                        }
                        break;
                    }
            }
        }
        outputStream.Seek(0, SeekOrigin.Begin);

        Console.WriteLine($"Performing RLE decompression from 0x{startPosition:X4} to 0x{endPosition:X4} ({endPosition - startPosition} bytes). Output = {outputStream.Length} bytes.");

        return outputStream;
    }
}