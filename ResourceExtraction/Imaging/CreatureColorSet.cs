namespace ResourceExtraction.Imaging;

using GameData.Resources.Image;
using System;
using System.IO;

/// <summary>The creature recolor primitive — an <b>internal implementation detail</b> of the extraction
/// layer that never surfaces in GameData. Mirrors the original <c>ApplyColorSetToBitmap</c>: a variant key
/// <c>&lt;stem&gt;_CS&lt;n&gt;</c> selects a base BMX plus the 256-entry index→index LUT from
/// <c>CS&lt;n&gt;.DAT</c>, which is applied to every sub-image's indices.
///
/// <para>This class only does the <b>index remap</b>. Turning the recolored indices into pixels reuses the
/// normal image pipeline (<c>BitmapExtractor</c> → <c>ToRawImage</c> in the tool, or Unity's
/// <c>ImageConverter</c>) — just fed <see cref="CreaturePalette"/> instead of the usual palette. No new
/// image-conversion code.</para></summary>
public static class CreatureColorSet {
    /// <summary>Parse a variant key into its base bitmap stem and colorset.
    /// <c>"MOR1_CS2"</c> → <c>("MOR1", 2)</c>; <c>"GOR1"</c> → <c>("GOR1", -1)</c> (no recolor).</summary>
    public static (string baseStem, int colorSet) ParseVariantKey(string key) {
        int idx = key.LastIndexOf("_CS", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0 && int.TryParse(key[(idx + 3)..], out int cs)) {
            return (key[..idx], cs);
        }
        return (key, -1);
    }

    /// <summary>CSx.DAT is a raw 256-byte index→index lookup table (identity baseline, selective
    /// color-block substitution). Returns the 256-entry table.</summary>
    public static byte[] ReadLut(Stream csDat) {
        var lut = new byte[256];
        int read = 0;
        while (read < 256) {
            int n = csDat.Read(lut, read, 256 - read);
            if (n == 0) {
                throw new EndOfStreamException("CSx.DAT shorter than 256 bytes");
            }
            read += n;
        }
        return lut;
    }

    /// <summary>Apply the LUT to every sub-image's index data, in place — <c>pixel = lut[pixel]</c>
    /// across the whole set.</summary>
    /// <remarks>
    /// <b>Verified against the original</b> — <c>ApplyColorSetToBitmap</c> @0x1657c does exactly
    /// <c>pixel = lut[pixel]</c>, so the direction here is right (it is a substitution, not an
    /// inverse mapping; getting that backwards is the obvious way to be wrong and we are not).
    ///
    /// <para>Two things the original does that this does NOT, both deliberate:</para>
    /// <list type="bullet">
    /// <item>The original walks the <b>RLE stream</b> and remaps only the pixel bytes, stepping over
    /// the control bytes (high bit set = one literal, else a run of <c>b &amp; 0x7F</c>). We run
    /// <b>after</b> <c>BitmapExtractor.Extract</c> has decoded the image, so every byte we see is
    /// already a pixel and a blanket loop is equivalent. Do not move this call before the decode —
    /// it would rewrite the control bytes and shred the image.</item>
    /// <item>The original skips a bitmap that is not flagged compressed, recolouring nothing. We
    /// recolour unconditionally. Inert in practice: no creature BMX in the shipped archive has an
    /// uncompressed frame (checked across all 4244 BMX frames), so the branch is never taken.</item>
    /// </list>
    /// </remarks>
    public static void Apply(ImageSet set, byte[] lut) {
        if (lut.Length != 256) {
            throw new ArgumentException("colorset LUT must be 256 entries", nameof(lut));
        }
        foreach (BmImage image in set.Images) {
            byte[]? data = image.BitMapData;
            if (data == null) {
                continue;
            }
            for (int i = 0; i < data.Length; i++) {
                data[i] = lut[data[i]];
            }
        }
    }
}
