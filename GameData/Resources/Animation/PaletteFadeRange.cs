namespace GameData.Resources.Animation;

/// <summary>
/// Which palette entries a cutscene fade actually touches, given the command's
/// <c>Start</c>/<c>Length</c> and the palette in hand.
/// </summary>
/// <remarks>
/// <b>Lifted from two byte-identical copies</b> in the fade-in and fade-out command handlers, whose
/// own comments named that duplication as the drift this test task exists to remove. Same rules,
/// same bugs, twice.
///
/// <para><b><c>Length</c> IS A COUNT, NOT A LAST COLOUR — and that was worth checking.</b> The
/// original's <c>palette_set_scaled(first_color, last_color, …)</c> takes a last colour, and
/// canassa's <c>palette_fade_in(palette_off, palette_seg, …)</c> passes its first two arguments
/// straight into it, so its parameter names are wrong twice over. That made "our Length is really a
/// last colour" a live hypothesis. <b>The shipped data disproves it:</b> the commands include
/// <c>Start=228 Length=28</c>, which as a last colour would be an inverted range, and most pairs sum
/// to 256 (<c>16+240</c>, <c>1+255</c>, <c>208+48</c>) — the signature of a count that reaches the
/// end. Recorded so nobody re-derives the hypothesis from the misnamed original.</para>
/// </remarks>
public static class PaletteFadeRange {
    /// <summary>The <c>Length</c> the shipped scripts use to mean "the whole palette".</summary>
    /// <remarks>
    /// <b>Only distinguishable from the ordinary clamp when the palette is not 256 entries.</b> With
    /// a 256-entry palette, <c>(0, 256)</c> resolves the same way through the sentinel and through
    /// the length clamp — so this branch is inert on VGA data and exists for the case where it is
    /// not. It is kept because the scripts genuinely spell "everything" this way 41 times, and a
    /// reader deleting it as dead code would silently change behaviour for a shorter palette.
    /// </remarks>
    public const int WholePaletteLength = 256;

    /// <summary>The resolved range, or <see cref="Valid"/> = false when the start is out of bounds.</summary>
    public readonly struct Range {
        /// <summary>First entry to fade.</summary>
        public int Start { get; }

        /// <summary>How many entries to fade, clamped to the palette.</summary>
        public int Length { get; }

        /// <summary>Whether the command names a usable range at all.</summary>
        public bool Valid { get; }

        /// <summary>Whether the length was reduced to fit.</summary>
        public bool Clamped { get; }

        internal Range(int start, int length, bool valid, bool clamped) {
            Start = start;
            Length = length;
            Valid = valid;
            Clamped = clamped;
        }
    }

    /// <summary>
    /// Resolve a command's range against the palette it will be applied to.
    /// </summary>
    /// <remarks>
    /// <b>An out-of-range START is refused, an over-long LENGTH is clamped</b>, and the asymmetry is
    /// the original behaviour rather than an oversight: a start past the end names no entries at all,
    /// while an over-long run still names real ones and simply stops at the end. Both handlers
    /// already did this; stating it here is what makes it checkable.
    /// </remarks>
    public static Range Resolve(int start, int length, int paletteSize) {
        if (paletteSize <= 0 || start < 0 || start >= paletteSize) {
            return new Range(start, 0, valid: false, clamped: false);
        }

        int resolved = start == 0 && length == WholePaletteLength ? paletteSize : length;
        if (resolved < 0) {
            return new Range(start, 0, valid: false, clamped: false);
        }

        if (start + resolved > paletteSize) {
            return new Range(start, paletteSize - start, valid: true, clamped: true);
        }
        return new Range(start, resolved, valid: true, clamped: false);
    }
}
