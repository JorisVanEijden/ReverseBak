namespace ResourceExtraction.Imaging;

/// <summary>A finished, palette-resolved RGBA32 image (row-major, top-to-bottom) — the engine-independent
/// "finished sprite" the creature pipeline emits. Consumers (the PNG tool, Unity) wrap it into their own
/// image type. Plain struct (no <c>init</c>) so it compiles under the netstandard2.1 plugin target.</summary>
public readonly struct RgbaImage {
    public int Width { get; }
    public int Height { get; }
    public byte[] Rgba { get; }

    public RgbaImage(int width, int height, byte[] rgba) {
        Width = width;
        Height = height;
        Rgba = rgba;
    }
}
