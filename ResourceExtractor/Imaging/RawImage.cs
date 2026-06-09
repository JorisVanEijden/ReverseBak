namespace ResourceExtractor.Imaging;

/// <summary>Platform-independent RGBA32 image (row-major, top-to-bottom) — the cross-platform
/// replacement for <c>System.Drawing.Bitmap</c> in the extractor's PNG pipeline.</summary>
public sealed class RawImage(int width, int height) {
    public int Width { get; } = width;
    public int Height { get; } = height;
    public byte[] Rgba { get; } = new byte[width * height * 4];

    public void SetPixel(int x, int y, byte r, byte g, byte b, byte a) {
        int o = (y * Width + x) * 4;
        Rgba[o] = r;
        Rgba[o + 1] = g;
        Rgba[o + 2] = b;
        Rgba[o + 3] = a;
    }
}
