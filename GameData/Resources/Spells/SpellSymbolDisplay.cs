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
    /// <b>THE COLOUR ARGUMENTS BELOW NEVER REACH THE SCREEN.</b>
    /// </summary>
    /// <remarks>
    /// Established 2026-08-19 by reading the blitter rather than the caller. SPELL.FNT declares
    /// glyph format 3 in its header — a byte per pixel — and <c>drawGlyphClipped</c> (0x15c48)
    /// assigns <c>textColor = thatByte</c> for every byte of 5 or more before drawing it. The font's
    /// ink bytes are 0, 6, 35, 108 and 110, so <i>every</i> pixel it draws overwrites the colour the
    /// caller chose, and none of them fall in the 1..4 range that would be remapped instead.
    ///
    /// <para>So the fade-in produces a DELAY and not a fade — the seven passes still wait seven
    /// ticks each, and the symbols still appear gradually in the sense of arriving late, but they
    /// never change colour. And <see cref="SelectedColour"/>'s shimmer marks nothing at all: the
    /// selected symbol draws exactly like its neighbours.</para>
    ///
    /// <para>The rules are kept rather than deleted because they are what the routine <i>says</i>,
    /// and a font with pens below 5 — a mod's, or a different symbol font — would obey them. But a
    /// port must not build a highlight out of them and call it faithful: for the shipped data there
    /// is no colour highlight on the casting ring to reproduce.</para>
    /// </remarks>
    public static bool ColourAppliesToTheShippedSymbolFont => false;

    /// <summary>The lowest glyph byte that is used as a pen outright.</summary>
    /// <remarks>Below this the blitter substitutes an entry from its own five-colour table.</remarks>
    public const int LowestLiteralPen = 5;

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
    /// <remarks>
    /// <b>A HARD-CODED 10, not the glyph's own height.</b> <c>cspell_menu_animate_hilite</c> sets
    /// <c>iHeight = 10</c> once and subtracts <c>iHeight &gt;&gt; 1</c> from every symbol's Y, so
    /// every glyph is lifted by the SAME five pixels whatever its size — while the X offset really
    /// is half the measured width. The two axes are centred differently and it looks like an
    /// oversight in the original; reproducing it is the difference between the game's ring and a
    /// tidier one.
    ///
    /// <para>SPELL.FNT's glyphs are pictures, so most are taller than ten pixels: centring
    /// vertically on the glyph instead lifts every symbol too far, by half its own height less
    /// five.</para>
    /// </remarks>
    public const int LineBox = 10;

    /// <summary>Canonical-space vertical scale — VGA x6 down.</summary>
    /// <remarks>
    /// Converting here rather than at the call site is the house rule, the same one
    /// <see cref="Dialog.DialogButtonRow.CanonicalScaleY"/> states: it keeps the 320x200 space out
    /// of the UI layer.
    /// </remarks>
    public const int CanonicalScaleY = 6;

    /// <summary>Half the line box, in canonical pixels — what a renderer passes to
    /// <see cref="GlyphOrigin"/>.</summary>
    public const int HalfLineBoxCanonical = LineBox / 2 * CanonicalScaleY;

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
    /// The multiplier the settled draw uses, after the fade has finished.
    /// </summary>
    /// <remarks>
    /// <b>Twelve, not the fade's last pass.</b> The fade brightens to
    /// <c>base + 6 * step</c> and the settled draw then jumps to <c>base + 12 * step</c>, so the
    /// symbols land brighter than the animation left them. Reusing the last fade colour as the
    /// resting colour leaves the whole ring dimmer than the game shows it.
    /// </remarks>
    public const int SettledColourMultiplier = 12;

    /// <summary>The colour an unselected symbol rests at.</summary>
    public static int SettledColour(int baseColour, int colourStep) =>
        baseColour + (colourStep * SettledColourMultiplier);

    /// <summary>First colour of the selected symbol's cycle.</summary>
    public const int SelectionColourBase = 208;

    /// <summary>Colours the selection cycles through.</summary>
    public const int SelectionColourCount = 8;

    /// <summary>Ticks the selection holds each colour.</summary>
    public const int SelectionTicksPerColour = 4;

    /// <summary>
    /// The colour of the selected symbol on a given tick.
    /// </summary>
    /// <remarks>
    /// <b>The selected symbol shimmers; it is not merely a different colour.</b> It cycles through
    /// eight colours from <see cref="SelectionColourBase"/>, advancing every four ticks off a global
    /// counter the routine increments as it draws. A port that paints the selection one fixed colour
    /// loses the only thing marking it as live.
    /// </remarks>
    public static int SelectedColour(int tick) =>
        SelectionColourBase + ((tick / SelectionTicksPerColour) % SelectionColourCount);

    /// <summary>
    /// <b>The spell font is selected for the draw and the game font restored on the way out.</b>
    /// </summary>
    /// <remarks>
    /// Anything drawing text after this routine gets the normal font back. A port that leaves the
    /// spell font active finds the next label rendered in spell glyphs.
    /// </remarks>
    public static bool RestoresTheGameFont => true;

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
