namespace GameData.Resources.Dialog;

using System.Collections.Generic;

/// <summary>
/// Decodes DDX inline formatting into styled runs — <c>drawCharacter</c> @0x15eef-0x15fa5.
///
/// <para>DDX text carries six formatting control bytes (0xF0-0xF5) inline, which surface as CP437
/// glyphs once the text is decoded. They are a small stateful machine over italic and the current
/// pen, not a markup language: two of them REMAP the pen relative to whatever it currently is, so a
/// consumer cannot interpret one in isolation.</para>
///
/// <para><b>What this does not do.</b> Turning a pen into a colour and a run into markup is the
/// consumer's job — this layer never sees a palette. It answers only "which characters are styled
/// how".</para>
/// </summary>
public static class DialogTextRuns {
    /// <summary>0xF0 — italic off, pen back to the body default.</summary>
    public const char Reset = '≡';

    /// <summary>0xF1 — italic on, with a pen shift.</summary>
    public const char ItalicHighlight = '±';

    /// <summary>0xF2 — identical to <see cref="ItalicHighlight"/>; the original shares the case.</summary>
    public const char ItalicHighlightAlt = '≥';

    /// <summary>0xF3 — italic on, pen untouched.</summary>
    public const char Italic = '≤';

    /// <summary>0xF4 — pen remap, applied twice (the original's case 4 falls through to case 5).</summary>
    public const char RemapTwice = '⌠';

    /// <summary>0xF5 — pen remap, applied once.</summary>
    public const char RemapOnce = '⌡';

    /// <summary>
    /// The CP437 glyphs of bytes 0xE0-0xFF, in byte order. <b>The original draws none of them.</b>
    /// </summary>
    /// <remarks>
    /// <c>font_render_glyph_or_ctrl</c> (canassa GFX/FONT/FONT.C:217-218) takes any byte whose high
    /// nibble is 0xE0 OR 0xF0 as a control code and switches on the low nibble alone, so 0xE3 is the
    /// same italic as 0xF3; nibbles 6-15 have no arm and draw nothing. The telepathic voices of
    /// chapter 8's Seven Pillars put 0xE3 before every letter, and with only 0xF0-0xF5 known here
    /// they read "πWπe πsπeπvπeπn".
    /// </remarks>
    private const string ControlGlyphs =
        "αßΓπΣσµτΦΘΩδ∞φε∩"
        + "≡±≥≤⌠⌡÷≈°∙·√ⁿ²■ ";

    /// <summary>The control code's low nibble — the only part the original switches on — or -1.</summary>
    private static int ControlNibble(char c) {
        int index = ControlGlyphs.IndexOf(c);
        return index < 0 ? -1 : index & 0x0F;
    }

    /// <summary>Whether a character is a control code (a CP437 byte 0xE0-0xFF) rather than text.</summary>
    /// <remarks><b>Deliberately callerless.</b> Production decodes through <see cref="Decode"/>, which switches on the codes itself; this states the set for the tests.</remarks>
    public static bool IsControlCode(char c) => ControlNibble(c) >= 0;

    /// <summary>A maximal stretch of source characters sharing one style.</summary>
    public readonly struct Run {
        public Run(int start, int length, bool italic, int pen) {
            Start = start;
            Length = length;
            Italic = italic;
            Pen = pen;
        }

        /// <summary>Index into the source string.</summary>
        public int Start { get; }

        /// <summary>Characters covered. Control codes are never included.</summary>
        public int Length { get; }

        public bool Italic { get; }

        /// <summary>Pen index; equal to the body pen when unstyled.</summary>
        public int Pen { get; }
    }

    /// <summary>
    /// Splits <c>[start, end)</c> into styled runs, dropping the control codes.
    /// </summary>
    /// <remarks>
    /// <b>A RANGE, not the whole string, because the wrap runs first.</b> That mirrors the original:
    /// <c>font_DrawWrappedTextBlock</c> calls <c>drawTextString</c> once per wrapped line
    /// (@0x4bad9), so each line is styled from the default pen on its own. It is safe here for the
    /// same reason it is safe there — state resets at every space and newline, so no style can span
    /// a break.
    ///
    /// <para><b>Space and newline reset italic AND pen.</b> That is the original's behaviour and it
    /// is what makes styling a line at a time equivalent to styling the whole block.</para>
    /// </remarks>
    public static List<Run> Decode(string text, int start, int end, int bodyPen) {
        var runs = new List<Run>();
        if (string.IsNullOrEmpty(text) || end <= start) {
            return runs;
        }

        var italic = false;
        int pen = bodyPen;
        int runStart = -1;
        var runItalic = false;
        int runPen = bodyPen;

        void Flush(int endExclusive) {
            if (runStart >= 0 && endExclusive > runStart) {
                runs.Add(new Run(runStart, endExclusive - runStart, runItalic, runPen));
            }
            runStart = -1;
        }

        for (int i = start; i < end; i++) {
            char c = text[i];
            switch (ControlNibble(c)) {
                case 0:     // Reset
                    Flush(i);
                    italic = false;
                    pen = bodyPen;
                    continue;
                case 1:     // ItalicHighlight
                case 2:     // ItalicHighlightAlt
                    // Pen 1 drops to 0, pen 0x0A stays, everything else becomes the highlight
                    // (0x15ef4-0x15f1e). For the common black-bodied dialog that is pen 5 — the
                    // cream highlight on the chapter-intro title.
                    Flush(i);
                    italic = true;
                    pen = pen == 1 ? 0 : pen == 0x0A ? 0x0A : 5;
                    continue;
                case 3:     // Italic
                    Flush(i);
                    italic = true;
                    continue;
                case 4:     // RemapTwice
                    // The original's case 4 falls THROUGH into case 5, so the remap runs twice.
                    Flush(i);
                    pen = RemapPen(RemapPenFirstStep(pen));
                    continue;
                case 5:     // RemapOnce
                    Flush(i);
                    pen = RemapPen(pen);
                    continue;
                case -1:
                    break;
                default:
                    // Nibbles 6-15: no arm in the original, so nothing is drawn and no style moves.
                    Flush(i);
                    continue;
            }

            switch (c) {
                case ' ':
                case '\n':
                    // Reset BEFORE the break character, so it belongs to the unstyled run that
                    // follows rather than trailing the styled word. Deliberately does NOT flush
                    // here: the shared logic below breaks the run only when the style actually
                    // changed, so a space inside already-unstyled text does not split it.
                    italic = false;
                    pen = bodyPen;
                    break;
                default:
                    break;
            }

            if (runStart < 0) {
                runStart = i;
                runItalic = italic;
                runPen = pen;
            } else if (runItalic != italic || runPen != pen) {
                Flush(i);
                runStart = i;
                runItalic = italic;
                runPen = pen;
            }
        }

        Flush(end);
        return runs;
    }

    /// <summary>The single remap step (the original's case 5, @0x15f59-0x15fa5).</summary>
    private static int RemapPen(int pen) =>
        pen == 0 ? 1 : pen == 1 ? 0x0B : pen == 0x0A ? 0 : 1;

    /// <summary>The extra step case 4 performs before falling through.</summary>
    private static int RemapPenFirstStep(int pen) =>
        pen == 0 ? 0x0A : pen == 1 ? 0x0A : pen == 0x0A ? 1 : 0x0A;
}
