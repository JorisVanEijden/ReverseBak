namespace ResourceExtraction.Compression;

using System;
using System.IO;

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
            if (marker < 0) {
                // End of stream before the declared end. A caller that overstates the length would
                // otherwise spin here forever: ReadByte keeps answering -1, and -1 fell through to
                // the literal-run branch whose loop body never executes, so the position never
                // advanced and the loop never ended.
                break;
            }
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

        return outputStream;
    }
}