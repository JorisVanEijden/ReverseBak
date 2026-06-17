namespace ResourceExtraction.Imaging;

using GameData.Resources.Image;
using System;
using System.IO;

/// <summary>The creature recolor mechanism — an <b>internal implementation detail</b> of the extraction
/// layer that never surfaces in GameData. Mirrors the original <c>PrepareCreatureBitmap</c> /
/// <c>ApplyColorSetToBitmap</c>: a creature variant key <c>&lt;stem&gt;_CS&lt;n&gt;</c> resolves to a base
/// BMX plus the 256-entry index→index LUT from <c>CS&lt;n&gt;.DAT</c>, applied to every sub-image.</summary>
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
    /// across the whole set (matches the original's set-wide <c>ApplyColorSetToBitmap</c>).</summary>
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
