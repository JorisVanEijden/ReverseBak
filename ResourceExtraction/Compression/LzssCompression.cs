namespace ResourceExtraction.Compression;

using ResourceExtraction.Extensions;

using System;
using System.IO;

internal class LzssCompression : ICompression {
    public Stream Compress(Stream inputStream) {
        throw new NotImplementedException();
    }

    public Stream Decompress(Stream inputStream, long length = 0) {
        var outputStream = new MemoryStream();
        long endPosition = length == 0 ? inputStream.Length : inputStream.Position + length;
        long startPosition = inputStream.Position;
        var buffer = new byte[261];
        
        while (inputStream.Position < endPosition) {
            int marker = inputStream.ReadByte();
            byte bit = 0x01;
            while (bit > 0 && inputStream.Position < endPosition) {
                if ((marker & bit) == bit) {
                    int data = inputStream.ReadByte();
                    outputStream.WriteByte((byte)data);
                } else {
                    int offset = inputStream.ReadByte() | inputStream.ReadByte() << 8;
                    int size = inputStream.ReadByte() + 5;
                    long originalPosition = outputStream.Position;
                    outputStream.Seek(offset, SeekOrigin.Begin);
                    outputStream.ReadExactly(buffer, 0, size);
                    outputStream.Seek(originalPosition, SeekOrigin.Begin);
                    outputStream.Write(buffer, 0, size);
                }
                bit <<= 1;
            }
        }

        // Console.WriteLine($"Output = {outputStream.Start} bytes.");
        outputStream.Seek(0, SeekOrigin.Begin);

        return outputStream;
    }
}