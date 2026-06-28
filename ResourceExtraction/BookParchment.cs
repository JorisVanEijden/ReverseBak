namespace ResourceExtraction;

using GameData.Resources.Image;

using ResourceExtraction.Imaging;

using System;

/// <summary>
/// The DOS book renderer (bok_DrawPage @ 0x4d4d4) draws the parchment background BOOK.SCX
/// vertically mirrored for odd page numbers and as-is for even ones — a page-number-parity
/// scheme computed in code, not a field in the BOK data.
///
/// Rather than reproduce that runtime flip in the engine, the extractor surfaces it as two
/// engine-independent images:
/// <list type="bullet">
///   <item><see cref="Even"/> (BOOK_EVEN.SCX) — BOOK.SCX as-is.</item>
///   <item><see cref="Odd"/> (BOOK_ODD.SCX) — BOOK.SCX flipped vertically.</item>
/// </list>
/// The Unity port picks one by page parity; modders can override either independently by
/// dropping their own BOOK_EVEN.SCX / BOOK_ODD.SCX in the overrides folder.
/// </summary>
public static class BookParchment {
    public const string Source = "BOOK.SCX";
    public const string Even = "BOOK_EVEN.SCX";
    public const string Odd = "BOOK_ODD.SCX";

    public static bool IsVariant(string resourceId) =>
        string.Equals(resourceId, Even, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(resourceId, Odd, StringComparison.OrdinalIgnoreCase);

    /// <summary>Build a variant from the already-extracted source BOOK.SCX image.</summary>
    public static BackgroundImage Build(string variantId, BackgroundImage source) {
        if (source == null) throw new ArgumentNullException(nameof(source));

        bool flipVertically = string.Equals(variantId, Odd, StringComparison.OrdinalIgnoreCase);
        byte[]? data = source.BitMapData;
        if (flipVertically && data != null) {
            data = ImageTransforms.FlipVerticalRows(data, source.Width, source.Height);
        }

        return new BackgroundImage(variantId) {
            Width = source.Width,
            Height = source.Height,
            Filename = source.Filename,
            BitMapData = data,
        };
    }
}
