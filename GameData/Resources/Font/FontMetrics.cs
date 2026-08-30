namespace GameData.Resources.Font;

/// <summary>
/// How wide a string is in one of the game's own fonts —
/// <c>getStringWidthInPixels</c> (@0x15be5), reached through <c>calculateTextWidth</c> (@0x15bd4).
///
/// <para>The engine measures text in exactly one place, and everything that sizes itself to its
/// label goes through it: the dialog button row's uniform width
/// (<see cref="Dialog.DialogButtonRow.ButtonWidth"/>), the menu entries, the keyword grid.</para>
/// </summary>
public static class FontMetrics {
    /// <summary>
    /// The width of <paramref name="text"/> in <paramref name="font"/>, in original pixels.
    /// </summary>
    /// <remarks>
    /// <b>A plain sum of glyph widths — there is NO letter spacing, and no kerning.</b> The loop
    /// adds one width per character and nothing else, so a port that inserted even a single pixel
    /// between glyphs would widen every measured string by its length and push every
    /// width-fitted box out with it.
    ///
    /// <para><b>A character the font does not carry contributes NOTHING.</b> The original range-checks
    /// against the font's first character and its count and skips out-of-range codes without
    /// substituting a fallback glyph — so measuring a string with characters outside the font
    /// silently under-measures rather than failing. That is faithful, and it is why this returns a
    /// width rather than reporting the miss.</para>
    ///
    /// <para>The original has a second mode for fonts with no per-character width table, where every
    /// character counts the same fixed width. No shipped .FNT takes it: the extractor reads a width
    /// for every glyph, so <see cref="FontResource.GlyphFor"/> answers for all of them. If a font
    /// ever arrives without them, this measures 0 rather than silently inventing a width.</para>
    /// </remarks>
    public static int TextWidth(FontResource font, string text) {
        if (font == null || string.IsNullOrEmpty(text)) {
            return 0;
        }

        var width = 0;
        foreach (char c in text) {
            FontGlyph glyph = font.GlyphFor(c);
            if (glyph != null) {
                width += glyph.Width;
            }
        }
        return width;
    }

    /// <summary>
    /// The widest of several strings, which is what a uniform row of buttons is sized from.
    /// </summary>
    /// <remarks>
    /// <b>One width for the whole row.</b> <c>CreateMenuEntriesFromDialogData</c> keeps a running
    /// maximum over every label and gives every button that same width, so the buttons are uniform
    /// rather than fitted to their own text — see <see cref="Dialog.DialogButtonRow"/>.
    /// </remarks>
    public static int WidestOf(FontResource font, System.Collections.Generic.IEnumerable<string> texts) {
        var widest = 0;
        if (texts == null) {
            return widest;
        }
        foreach (string text in texts) {
            int width = TextWidth(font, text);
            if (width > widest) {
                widest = width;
            }
        }
        return widest;
    }
}
