namespace ResourceExtraction.Imaging;

using GameData.Resources.Layout;

/// <summary>
/// Converts the DOS game's non-square pixels into square pixels at the extraction boundary,
/// so every downstream consumer receives correctly-proportioned, engine-independent assets and
/// needs no knowledge of the original VGA/EGA display.
///
/// VGA mode 13h (320x200) was shown on a 4:3 CRT, giving a 5:6 pixel aspect (pixels are 1.2x
/// taller than wide). Replicating each pixel x5 horizontally and x6 vertically yields exact 4:3
/// (1600x1200) with no interpolation — the 6/5 factor is integer-clean. The single 640x350 hi-res
/// screen (BOOK.SCX) corrects to 1280x960 (x2 horizontally; vertical resamples 350->960, because
/// the EGA factor 48/35 has no small-integer form). See memory: project-aspect-correction.
/// </summary>
public static class AspectCorrection {
    /// <summary>Horizontal replication factor for VGA (320x200-space) pixels.</summary>
    public const int VgaScaleX = 5;

    /// <summary>Vertical replication factor for VGA (320x200-space) pixels.</summary>
    public const int VgaScaleY = 6;

    /// <summary>Horizontal replication factor for EGA (640x350-space) pixels.</summary>
    public const int EgaScaleX = 2;

    /// <summary>Width of the original VGA mode 13h framebuffer.</summary>
    public const int VgaWidth = 320;

    /// <summary>Height of the original VGA mode 13h framebuffer.</summary>
    public const int VgaHeight = 200;

    /// <summary>Canonical frame width — derived from the mode and the aspect factor, never
    /// written as a literal, so changing a factor changes every consumer coherently.</summary>
    public static int CanonicalWidth => VgaWidth * VgaScaleX;

    /// <summary>Canonical frame height. See <see cref="CanonicalWidth"/>.</summary>
    public static int CanonicalHeight => VgaHeight * VgaScaleY;

    /// <summary>
    /// A fresh <see cref="DesignFrame"/> for the canonical space, at the faithful <see
    /// cref="LayoutFit.Contain"/> default. The one place extractors build a screen's frame from,
    /// so every screen resource derives it the same way instead of repeating the two fields.
    /// </summary>
    public static DesignFrame CanonicalFrame() => new() { Width = CanonicalWidth, Height = CanonicalHeight };

    /// <summary>Scales a VGA (320x200-space) X coordinate or width into canonical 1600x1200 space.</summary>
    public static int ScaleVgaX(int x) => x * VgaScaleX;

    /// <summary>Scales a VGA (320x200-space) Y coordinate or height into canonical 1600x1200 space.</summary>
    public static int ScaleVgaY(int y) => y * VgaScaleY;

    /// <summary>Scales an EGA (640x350-space) X coordinate or width into canonical 1280x960 space.</summary>
    public static int ScaleEgaX(int x) => x * EgaScaleX;

    /// <summary>
    /// Scales an EGA (640x350-space) Y coordinate or height into canonical 1280x960 space.
    /// Uses the same 96/35 vertical resample as <see cref="CorrectEga"/> so coordinates and pixels align.
    /// </summary>
    public static int ScaleEgaY(int y) => (int)System.Math.Round(y * 96.0 / 35.0);

    /// <summary>
    /// Nearest-neighbour resamples an 8-bpp bitmap (one byte per palette index) to the given
    /// destination size. Operating on palette indices keeps the palette-swap shader pipeline intact.
    /// When <paramref name="columnMajor"/> is set the data is stored column-major (BMX images with the
    /// ReversedRowColumn flag) and is replicated along that axis order; row order is preserved either
    /// way, so the result reads back identically through the existing converter.
    /// </summary>
    public static byte[] ResampleNearest(byte[] source, int sourceWidth, int sourceHeight, int destWidth, int destHeight, bool columnMajor = false) {
        var dest = new byte[destWidth * destHeight];
        for (var dy = 0; dy < destHeight; dy++) {
            int sy = dy * sourceHeight / destHeight;
            for (var dx = 0; dx < destWidth; dx++) {
                int sx = dx * sourceWidth / destWidth;
                int sourceIndex = columnMajor ? sx * sourceHeight + sy : sy * sourceWidth + sx;
                int destIndex = columnMajor ? dx * destHeight + dy : dy * destWidth + dx;
                dest[destIndex] = source[sourceIndex];
            }
        }

        return dest;
    }

    /// <summary>
    /// Corrects a VGA (320x200-space) bitmap to square pixels by x5 / x6 nearest-neighbour
    /// replication. Returns the new bitmap and reports the corrected dimensions.
    /// </summary>
    public static byte[] CorrectVga(byte[] source, int width, int height, bool columnMajor, out int newWidth, out int newHeight) {
        newWidth = width * VgaScaleX;
        newHeight = height * VgaScaleY;

        return ResampleNearest(source, width, height, newWidth, newHeight, columnMajor);
    }

    /// <summary>
    /// Corrects an EGA (640x350-space) bitmap to square pixels. EGA pixels are 48/35 taller than
    /// wide; doubling horizontally and resampling vertically by 96/35 yields exact 4:3 (a full
    /// 640x350 page becomes 1280x960, matching BOOK.SCX). Book images share the 640x350 layout
    /// space, so this keeps every book asset at one consistent square-pixel scale.
    /// </summary>
    public static byte[] CorrectEga(byte[] source, int width, int height, bool columnMajor, out int newWidth, out int newHeight) {
        newWidth = width * 2;
        newHeight = (int)System.Math.Round(height * 96.0 / 35.0);

        return ResampleNearest(source, width, height, newWidth, newHeight, columnMajor);
    }
}
