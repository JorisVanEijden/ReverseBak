namespace ResourceExtraction.Tests.Imaging;

using System.IO;
using System.IO.Compression;
using ResourceExtractor.Imaging;
using Xunit;

/// <summary>Verifies the dependency-free, cross-platform PNG encoder produces a structurally valid PNG
/// whose pixels round-trip exactly. (Replaces the Windows-only System.Drawing image save.)</summary>
public class PngWriterTests {
    [Fact]
    public void Write_EncodesValidRgbaPng_ThatRoundTripsPixels() {
        var image = new RawImage(2, 2);
        image.SetPixel(0, 0, 10, 20, 30, 255);
        image.SetPixel(1, 0, 40, 50, 60, 128);
        image.SetPixel(0, 1, 70, 80, 90, 0);
        image.SetPixel(1, 1, 100, 110, 120, 255);

        using var ms = new MemoryStream();
        PngWriter.Write(ms, image);
        byte[] png = ms.ToArray();

        // Signature
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, png[..8]);

        // Walk chunks: IHDR first, IDAT present, IEND last.
        int pos = 8;
        byte[]? idat = null;
        int width = 0, height = 0, bitDepth = 0, colorType = 0;
        string lastType = "";
        while (pos < png.Length) {
            int len = (png[pos] << 24) | (png[pos + 1] << 16) | (png[pos + 2] << 8) | png[pos + 3];
            string type = System.Text.Encoding.ASCII.GetString(png, pos + 4, 4);
            byte[] data = png[(pos + 8)..(pos + 8 + len)];
            if (type == "IHDR") {
                width = (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3];
                height = (data[4] << 24) | (data[5] << 16) | (data[6] << 8) | data[7];
                bitDepth = data[8];
                colorType = data[9];
            } else if (type == "IDAT") {
                idat = data;
            }
            lastType = type;
            pos += 12 + len; // length(4) + type(4) + data + crc(4)
        }

        Assert.Equal(2, width);
        Assert.Equal(2, height);
        Assert.Equal(8, bitDepth);
        Assert.Equal(6, colorType); // RGBA
        Assert.Equal("IEND", lastType);
        Assert.NotNull(idat);

        // Inflate IDAT and verify scanlines: each starts with filter byte 0, then exact RGBA.
        using var inflated = new MemoryStream();
        using (var z = new ZLibStream(new MemoryStream(idat!), CompressionMode.Decompress)) {
            z.CopyTo(inflated);
        }
        byte[] raw = inflated.ToArray();
        int stride = 2 * 4;
        Assert.Equal((stride + 1) * 2, raw.Length);
        Assert.Equal(0, raw[0]);                 // row 0 filter
        Assert.Equal(0, raw[stride + 1]);        // row 1 filter
        // row 0 pixel (0,0) and (1,0)
        Assert.Equal(new byte[] { 10, 20, 30, 255, 40, 50, 60, 128 }, raw[1..(stride + 1)]);
        // row 1 pixel (0,1) and (1,1)
        Assert.Equal(new byte[] { 70, 80, 90, 0, 100, 110, 120, 255 }, raw[(stride + 2)..(2 * (stride + 1))]);
    }
}
