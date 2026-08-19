namespace GameData.Resources.Spells;

using System.Collections.Generic;

/// <summary>
/// The strip along the top of the travel screen — <c>UI_DrawActiveSpellSymbols</c> (ovr179
/// @0x6d238).
///
/// <para><b>It is not a message box.</b> It shows which timed spell effects are running, one
/// symbol each, on a small plaque. Nothing else about it is text: the "string" the original builds
/// is a run of <see cref="SpellSymbolDisplay">spell-font</see> glyph codes drawn as a word, which
/// is why it centres like text and why it needs the same font the casting ring does.</para>
///
/// <para>Companion to <see cref="SpellPaletteEvents"/>, which owns the mask this reads.</para>
/// </summary>
public static class SpellEffectCaption {
    /// <summary>
    /// The glyph drawn for an effect, as a character in the spell font.
    /// </summary>
    /// <remarks>
    /// <b>The shipped table holds each glyph MINUS ONE and the routine adds it back.</b> Copying
    /// the table's own bytes across draws nine symbols that are each one place early in the font —
    /// wrong shapes throughout, and plausible-looking ones, since every neighbour is also a symbol.
    ///
    /// <para><b>Effects 3 and 4 are the other way round.</b> The table is otherwise consecutive but
    /// pairs effect 3 with the higher glyph and effect 4 with the lower, so a port that computes
    /// the glyph from the effect id instead of reading the table swaps those two symbols.</para>
    /// </remarks>
    public static int GlyphFor(int effectId) =>
        effectId >= 0 && effectId < SpellPaletteEvents.Count ? StoredGlyphs[effectId] + 1 : 0;

    /// <summary>The table as it ships, each entry one below the glyph actually drawn.</summary>
    private static readonly int[] StoredGlyphs =
        { 0x1d, 0x1e, 0x1f, 0x21, 0x20, 0x22, 0x23, 0x24, 0x25 };

    /// <summary>
    /// The glyphs to draw for a mask, in effect order.
    /// </summary>
    /// <remarks>
    /// <b>Effect order, not the order they were cast in.</b> The original walks the nine bits from
    /// zero, so the symbols keep a stable position relative to each other however the effects came
    /// and went — an effect ending shifts the rest along rather than leaving a gap.
    /// </remarks>
    public static IReadOnlyList<int> Glyphs(int mask) {
        var glyphs = new List<int>();
        for (var effect = 0; effect < SpellPaletteEvents.Count; effect++) {
            if ((mask & SpellPaletteEvents.BitFor(effect)) != 0) {
                glyphs.Add(GlyphFor(effect));
            }
        }

        return glyphs;
    }

    /// <summary>The plaque the symbols sit on.</summary>
    public const string PlaqueImage = "CAST.BMX";

    /// <summary>Left edge of the plaque, in original pixels.</summary>
    public const int PlaqueX = 0x80;

    /// <summary>Top edge of the plaque, in original pixels.</summary>
    public const int PlaqueY = 2;

    /// <summary>
    /// The X the caption is centred on, in original pixels.
    /// </summary>
    /// <remarks>
    /// <b>The screen's centre, not the plaque's.</b> They agree here — the plaque is 64 wide at
    /// x 128 — but the original centres on the screen and would keep doing so if the art moved.
    /// </remarks>
    public const int TextCentreX = 160;

    /// <summary>Top of the caption text, in original pixels.</summary>
    public const int TextY = 1;

    /// <summary>
    /// Whether an empty mask still draws the plaque.
    /// </summary>
    /// <remarks>
    /// <b>Yes — the plaque is unconditional.</b> The blit happens before the bits are walked, so
    /// with nothing running the strip is still there and simply empty. A port that hides the whole
    /// widget when no effect is active loses a piece of the frame, not just its contents.
    /// </remarks>
    public static bool PlaqueDrawsWhenNothingIsActive => true;
}
