namespace GameData.Resources.Config;

using System;
using System.Collections.Generic;

/// <summary>
/// The colour remap the camp dial's shaded wedge is drawn through —
/// <c>encamp_drawSundialShadow</c> (ovr182 @0x70c9b) and the table at
/// <c>sub_ovr182_7E1</c> (@0x70bb1).
/// </summary>
/// <remarks>
/// <b>THE WEDGE IS NOT A FILL. IT REMAPS THE PIXELS UNDER IT.</b> The polygon filler's span
/// callback is swapped for one that reads each destination pixel, indexes a 256-byte table with it
/// and writes the result back — <c>sub_vmcode_2734</c> @0x7d792 is literally
/// <c>al = [di]; xlat; [di] = al</c>. So the shading is a per-colour substitution, and a flat
/// translucent overlay is a different thing: it would wash the moon and the stars along with the
/// ground, where the real one leaves untouched whatever the palette cannot darken.
///
/// <para>Where the wedge goes is <see cref="EncampDial"/>'s — this is only what it does to what is
/// already there.</para>
/// </remarks>
public static class EncampShadow {
    /// <summary>
    /// Palette entries the remap touches — <b>the low range, which is the opposite of the world
    /// lighting's.</b>
    /// </summary>
    /// <remarks>
    /// The dynamic-lighting system protects 0..111 and lights 112..255; this table does exactly the
    /// reverse. They are describing the same split from opposite sides: the low entries are the
    /// interface's, and the camp dial is interface art.
    /// </remarks>
    public const int RemappedEntries = 112;

    /// <summary>
    /// How much darker the shaded colour is asked to be, on every channel — <b>in the original's
    /// six-bit units</b>.
    /// </summary>
    /// <remarks>
    /// Ten of sixty-four, applied to red, green and blue alike: the wedge asks for a plain
    /// darkening and lets the search decide what the palette can actually offer.
    /// </remarks>
    public const int Darkening = 10;

    /// <summary>Largest channel value in the original's palette.</summary>
    public const int VgaChannelMax = 63;

    /// <summary>
    /// <see cref="Darkening"/> against a palette whose channels have been scaled up.
    /// </summary>
    /// <remarks>
    /// <b>The extractor widens VGA's six bits to eight</b> (<c>r &lt;&lt; 2 | r &gt;&gt; 4</c>), so a
    /// consumer holding 0..255 colours must widen the darkening too. Ten used raw against 0..255 is
    /// a quarter of the intended shade — a wedge you can barely see, which reads as "the remap does
    /// almost nothing" rather than as a units mistake.
    /// </remarks>
    public static int DarkeningFor(int channelMax) => Darkening * channelMax / VgaChannelMax;

    /// <summary>
    /// The colour a shaded pixel becomes: the entry nearest to this one darkened by
    /// <see cref="Darkening"/>.
    /// </summary>
    /// <remarks>
    /// <b>The search is Manhattan distance and it is confined to the same 112 entries.</b> The
    /// result is therefore not a uniform darkening: where the palette has no darker neighbour the
    /// nearest entry is the colour itself and that pixel does not change at all. Approximating the
    /// wedge as "multiply by a constant" gets the smooth parts right and the flat parts wrong, and
    /// the flat parts are most of the artwork.
    ///
    /// <para>The channels are clamped at zero before the search — a negative target would otherwise
    /// pull the match toward black harder than the palette warrants.</para>
    /// </remarks>
    public static int ShadedEntry(IReadOnlyList<(int R, int G, int B)> palette, int entry,
        int darkening = Darkening) {
        if (palette == null || entry < 0 || entry >= palette.Count) {
            return entry;
        }
        if (entry >= RemappedEntries) {
            // *** THE HIGH ENTRIES MAP TO THEMSELVES. *** The table's second half is the identity,
            // so anything drawn in those colours passes under the wedge unshaded.
            return entry;
        }

        (int r, int g, int b) = palette[entry];
        int wantR = Math.Max(0, r - darkening);
        int wantG = Math.Max(0, g - darkening);
        int wantB = Math.Max(0, b - darkening);

        int best = int.MaxValue;
        int bestEntry = entry;
        int limit = Math.Min(RemappedEntries, palette.Count);
        for (var candidate = 0; candidate < limit; candidate++) {
            (int cr, int cg, int cb) = palette[candidate];
            int distance = Math.Abs(wantR - cr) + Math.Abs(wantG - cg) + Math.Abs(wantB - cb);
            if (distance < best) {
                best = distance;
                bestEntry = candidate;
            }
        }

        return bestEntry;
    }

    /// <summary>
    /// <b>The search keeps the FIRST nearest entry, not the last.</b>
    /// </summary>
    /// <remarks>
    /// The original's comparison is strictly "less than", so a later candidate at the same distance
    /// does not displace an earlier one. With a palette that repeats colours — and the interface
    /// range does — a "less than or equal" test picks a different entry for the same input.
    /// </remarks>
    public static bool TiesKeepTheEarlierEntry => true;

    /// <summary>The whole table, as the original builds it once and keeps.</summary>
    public static int[] Table(IReadOnlyList<(int R, int G, int B)> palette,
        int darkening = Darkening) {
        var table = new int[256];
        for (var entry = 0; entry < table.Length; entry++) {
            table[entry] = ShadedEntry(palette, entry, darkening);
        }

        return table;
    }
}
