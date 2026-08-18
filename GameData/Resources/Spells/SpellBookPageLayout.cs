namespace GameData.Resources.Spells;

/// <summary>
/// Where the spellbook page puts its six rows — <c>charscreen_draw_spell_book_actor</c>
/// (<c>SRC/CHAR/CHARSCRN.C</c>). Canonical 1600x1200 (VGA x5 across, x6 down).
/// </summary>
/// <remarks>
/// <b>Every row is drawn, including the empty ones.</b> The box and its icon are painted before the
/// character's spells are even consulted, so a caster who knows one school sees six categories with
/// five blank lines rather than a shortened page — see <see cref="SpellBookPageView.Line"/>.
/// </remarks>
public static class SpellBookPageLayout {
    /// <summary>How many category rows the page has.</summary>
    public const int Rows = 6;

    /// <summary>
    /// The top of a row's box, counting rows from 1.
    /// </summary>
    /// <remarks>
    /// VGA <c>row * 0x20 - 0x1b</c>, so the first row starts at y=5 and they step 32 apart. Counted
    /// from ONE: starting at zero lifts the whole page 32 rows and hides the first box off the top.
    /// </remarks>
    public static int RowY(int rowFromOne) => ((rowFromOne * 0x20) - 0x1b) * 6;

    /// <summary>Left edge of a row's box — VGA x=6.</summary>
    public const int BoxX = 6 * 5;

    /// <summary>Box width — VGA 0x27.</summary>
    public const int BoxWidth = 0x27 * 5;

    /// <summary>Box height — VGA 0x1e.</summary>
    public const int BoxHeight = 0x1e * 6;

    /// <summary>The box's fill pen.</summary>
    public const int BoxFillPen = 0x11;

    /// <summary>The box's outline pen.</summary>
    public const int BoxOutlinePen = 0x8e;

    /// <summary>
    /// The pen the box's shadow is drawn in.
    /// </summary>
    /// <remarks>
    /// <b>The box has a drop shadow, and it is a whole second rectangle.</b> The original fills a
    /// black box at <c>(7, y+1)</c> — same size, one pixel down-right — BEFORE painting the real
    /// one, exactly as the text on this page is drawn twice. A port that draws only the second
    /// rectangle loses the depth every box on this screen has.
    /// </remarks>
    public const int BoxShadowPen = 0;

    /// <summary>How far the shadow is offset, horizontally — one original pixel.</summary>
    public const int ShadowOffsetX = 5;

    /// <summary>How far the shadow is offset, vertically — one original pixel.</summary>
    public const int ShadowOffsetY = 6;

    /// <summary>Left edge of the category icon — VGA x=9.</summary>
    public const int IconX = 9 * 5;

    /// <summary>The icon sits one original pixel below the box's top.</summary>
    public const int IconOffsetY = 6;

    /// <summary>
    /// The sprite set the category icons come from.
    /// </summary>
    /// <remarks>
    /// The BUTTON sprites in their up state (<c>g_pButtonSpriteUp[icon]</c>), which is the same set
    /// the menu screens draw their buttons from — not a spell-specific sheet. The index is
    /// <see cref="SpellBookGroup.Icon"/>, straight out of INVSPELL.DAT.
    /// </remarks>
    public const string IconSet = "BICONS1.BMX";

    /// <summary>Left edge of the row's spell list — VGA x=0x32.</summary>
    public const int TextX = 0x32 * 5;

    /// <summary>Width the list wraps within — VGA 0x109.</summary>
    public const int TextWidth = 0x109 * 5;

    /// <summary>Height the list is laid out in — VGA 0x1e.</summary>
    public const int TextHeight = 0x1e * 6;

    /// <summary>The pen the spell list is written in.</summary>
    public const int TextPen = 10;

    /// <summary>The pen behind it, one pixel down-right.</summary>
    /// <remarks>Drawn first, from <c>(0x33, y+1)</c>, the same shadow pass the rest of this screen
    /// family uses.</remarks>
    public const int TextShadowPen = 1;

    /// <summary>The palette the page's pens resolve against.</summary>
    public const string Palette = "INVENTOR.PAL";

    /// <summary>
    /// Whether the page waits for the player before anything else can happen.
    /// </summary>
    /// <remarks>
    /// It does — the original loops on <c>dialog_poll_arrow_or_button</c> until a key or a click,
    /// so the page is modal and the sheet beneath it is not interactive while it is up.
    /// </remarks>
    public const bool IsModal = true;
}
