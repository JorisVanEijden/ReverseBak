namespace GameData.Resources.Spells;

/// <summary>
/// How a school's spell symbols are drawn onto the casting ring — <c>UI_drawSpellSymbols</c>
/// (ovr173 @0x69252).
///
/// <para>Companion to <see cref="CastRingLayout"/>, which places the ring itself.</para>
/// </summary>
public static class SpellSymbolDisplay {
    /// <summary>
    /// <b>A spell symbol is a character in a font, not a bitmap.</b>
    /// </summary>
    /// <remarks>
    /// The routine selects a dedicated spell font and draws each symbol as a one-character string.
    /// The <c>FontGlyph</c> the extracted <c>SYMBOL&lt;n&gt;.DAT</c> carries per node is that
    /// character. A port that goes looking for a sprite sheet for these will not find one — and the
    /// distinction is not cosmetic, because the glyph's measured width is what centres it.
    /// </remarks>
    public static bool SymbolsAreFontGlyphs => true;

    /// <summary>
    /// <b>Only spells the caster can actually cast are drawn.</b>
    /// </summary>
    /// <remarks>
    /// Each symbol is gated on the same castable test the rest of the screen uses, so the ring shows
    /// this caster's repertoire rather than the school's. Drawing them all and greying the
    /// unavailable ones would be a different screen from the one the game shows.
    /// </remarks>
    public static bool OnlyCastableSymbolsAreDrawn => true;

    /// <summary>The line box the vertical centring halves, in original pixels.</summary>
    public const int LineBox = 10;

    /// <summary>
    /// Where a symbol's glyph is drawn, given its node position and the glyph's measured width.
    /// </summary>
    /// <param name="nodeX">The node's X, canonical.</param>
    /// <param name="nodeY">The node's Y, canonical.</param>
    /// <param name="glyphWidth">The measured width of the glyph, canonical.</param>
    /// <param name="halfLineBox">
    /// Half <see cref="LineBox"/> in the caller's units — canonical callers pass the scaled value,
    /// since the original's 5 is in its own pixels.
    /// </param>
    /// <remarks>
    /// <b>The stored position is the symbol's centre, not its corner.</b> The original subtracts half
    /// the measured text width and half the line box before drawing. Treating the node position as a
    /// top-left corner shifts every glyph right and down by half its own size — subtly wrong in a way
    /// that looks like a bad font rather than a bad offset.
    /// </remarks>
    public static (int X, int Y) GlyphOrigin(int nodeX, int nodeY, int glyphWidth, int halfLineBox) =>
        (nodeX - (glyphWidth / 2), nodeY - halfLineBox);

    /// <summary>Passes the fade-in runs.</summary>
    public const int FadePasses = 7;

    /// <summary>Timer ticks each pass waits before the next.</summary>
    public const int FadePassTicks = 7;

    /// <summary>
    /// The text colour for a fade pass.
    /// </summary>
    /// <remarks>
    /// The symbols are drawn seven times, brightening by <paramref name="colourStep"/> each pass with
    /// a wait between — so they fade in rather than appear. A port that draws them once gets the
    /// right picture and loses the entrance.
    /// </remarks>
    public static int FadeColour(int baseColour, int pass, int colourStep) =>
        baseColour + (pass * colourStep);

    /// <summary>
    /// Whether the fade runs at all.
    /// </summary>
    /// <remarks>
    /// <b>A colour step of zero skips the animation entirely</b> — the loop's own exit test — which is
    /// how the screen redraws symbols without replaying the fade. So the same routine is both "fade
    /// in" and "just draw", chosen by that argument.
    /// </remarks>
    public static bool FadeRuns(int colourStep) => colourStep != 0;
}
