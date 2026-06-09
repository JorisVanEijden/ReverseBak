namespace ResourceExtractor.Imaging;

using System;
using System.IO;
using System.IO.Compression;
using System.Text;

/// <summary>Minimal, dependency-free PNG encoder (8-bit RGBA, color type 6). Cross-platform: the IDAT
/// stream is produced by the BCL <see cref="ZLibStream"/>, replacing the Windows-only
/// <c>System.Drawing</c> image save so PNG extraction runs on Linux/macOS as well.</summary>
public static class PngWriter {
    private static readonly uint[] CrcTable = BuildCrcTable();

    public static void Write(string path, RawImage image) {
        using FileStream fs = File.Create(path);
        Write(fs, image);
    }

    public static void Write(Stream output, RawImage image) {
        ReadOnlySpan<byte> signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        output.Write(signature);

        var ihdr = new byte[13];
        WriteBe(ihdr, 0, (uint)image.Width);
        WriteBe(ihdr, 4, (uint)image.Height);
        ihdr[8] = 8; // bit depth
        ihdr[9] = 6; // color type: truecolour with alpha
        // ihdr[10..12] = compression/filter/interlace = 0
        WriteChunk(output, "IHDR", ihdr);

        // Filtered scanlines (filter type 0 = none), then zlib-compressed into one IDAT.
        int stride = image.Width * 4;
        var raw = new byte[(stride + 1) * image.Height];
        for (int y = 0; y < image.Height; y++) {
            int dst = y * (stride + 1);
            raw[dst] = 0;
            Array.Copy(image.Rgba, y * stride, raw, dst + 1, stride);
        }
        byte[] compressed;
        using (var ms = new MemoryStream()) {
            using (var z = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true)) {
                z.Write(raw, 0, raw.Length);
            }
            compressed = ms.ToArray();
        }
        WriteChunk(output, "IDAT", compressed);
        WriteChunk(output, "IEND", Array.Empty<byte>());
    }

    private static void WriteChunk(Stream s, string type, byte[] data) {
        Span<byte> len = stackalloc byte[4];
        WriteBe(len, 0, (uint)data.Length);
        s.Write(len);
        byte[] typeBytes = Encoding.ASCII.GetBytes(type);
        s.Write(typeBytes, 0, typeBytes.Length);
        s.Write(data, 0, data.Length);
        Span<byte> crc = stackalloc byte[4];
        WriteBe(crc, 0, Crc(typeBytes, data));
        s.Write(crc);
    }

    private static void WriteBe(Span<byte> buf, int offset, uint value) {
        buf[offset] = (byte)(value >> 24);
        buf[offset + 1] = (byte)(value >> 16);
        buf[offset + 2] = (byte)(value >> 8);
        buf[offset + 3] = (byte)value;
    }

    private static uint Crc(byte[] type, byte[] data) {
        uint c = 0xFFFFFFFF;
        foreach (byte b in type) c = CrcTable[(c ^ b) & 0xFF] ^ (c >> 8);
        foreach (byte b in data) c = CrcTable[(c ^ b) & 0xFF] ^ (c >> 8);
        return c ^ 0xFFFFFFFF;
    }

    private static uint[] BuildCrcTable() {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++) {
            uint c = n;
            for (int k = 0; k < 8; k++) {
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            }
            table[n] = c;
        }
        return table;
    }
}
