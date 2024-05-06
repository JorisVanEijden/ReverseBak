namespace ResourceExtraction.Extensions;

public static class StreamExtensions {
    public static void ReadExactly(this System.IO.Stream stream, byte[] buffer, int offset, int length) {
        var totalRead = 0;
        while (totalRead < length) {
            int read = stream.Read(buffer, offset + totalRead, length - totalRead);

            if (read == 0)
                throw new System.IO.EndOfStreamException();
            totalRead += read;
        }
    }

    public static void ReadExactly(this System.IO.Stream stream, byte[] buffer) {
        ReadExactly(stream, buffer, 0, buffer.Length);
    }
}