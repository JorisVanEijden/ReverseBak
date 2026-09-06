namespace GameData.Resources.Spells;

/// <summary>
/// The ring drawn around the party portrait of whoever is casting.
/// </summary>
/// <remarks>
/// <b>It is an image, not a drawn ellipse.</b> <c>CASTFACE.BMX</c> holds seven entries: 0..5 are the
/// ring beads and <b>6 is this highlight</b> — a 280x270 circle outline in blue-grey, at canonical
/// scale. <c>cspell_cast_menu_loop</c> blits it over the caster's face
/// (<c>emsimg_map_then_call_180c(&amp;g_pCastfaceSpriteTable[6]-&gt;wImageData, ...)</c>,
/// CSPELL.C:2207) and never draws a shape.
///
/// <para><b>The strip is regular; the REQ click areas are not.</b> The original saves and restores
/// the whole portrait band as one rect — <c>cga_save_rect_to_buffer(pDest, 0xf, 0x8e, 0xa8, 0x2d)</c>
/// — which is VGA (15, 142) 168x45. 168 is exactly three slots of 56, and 56x45 is exactly the
/// sprite, so the three slots tile that band with no gap. REQ_CAST's party click areas sit at
/// canonical x 70 / 365 / 660, which drift from this band by +5, -10 and -25 as you go along, so a
/// highlight positioned on the click areas is progressively wrong across the row. Measured on the
/// original's own render (2026-09-06): the ink around the middle portrait starts at canonical x 360,
/// which this band predicts (355 plus the sprite's one-pixel transparent margin) and the click area
/// does not (365).</para>
///
/// <para>Saving the band rather than one slot is also why switching caster mid-screen restores all
/// three: the highlight is painted onto the portraits, so moving it means putting the clean strip
/// back first.</para>
/// </remarks>
public static class CastCasterHighlight {
    /// <summary>The <c>CASTFACE.BMX</c> entry the original blits — index 6, after the six beads.</summary>
    public const int Icon = 6;

    /// <summary>The addressables key for that entry.</summary>
    public static string SpriteKey => $"{CastRingLayout.IconSet}#{Icon}";

    /// <summary>Left edge of the portrait band, VGA — the <c>0xf</c> of the save rect.</summary>
    private const int BandLeftVga = 0xf;

    /// <summary>Top edge of the portrait band, VGA — the <c>0x8e</c> of the save rect.</summary>
    private const int BandTopVga = 0x8e;

    /// <summary>Width of the whole band, VGA — the <c>0xa8</c> of the save rect.</summary>
    private const int BandWidthVga = 0xa8;

    /// <summary>Height of the band, VGA — the <c>0x2d</c> of the save rect, and the sprite's own.</summary>
    private const int SlotHeightVga = 0x2d;

    /// <summary>One slot's width, VGA. The band is exactly three of these.</summary>
    private const int SlotWidthVga = BandWidthVga / Character.ActiveParty.Slots;

    /// <summary>
    /// Where the highlight goes for a party slot, in canonical units.
    /// </summary>
    /// <remarks>
    /// Converted here rather than in the renderer, the house rule — see
    /// <see cref="Dialog.KeywordMenu.CanonicalScaleX"/>.
    /// </remarks>
    public static (int X, int Y, int Width, int Height) SlotRect(int slot) => (
        (BandLeftVga + (slot * SlotWidthVga)) * Dialog.KeywordMenu.CanonicalScaleX,
        BandTopVga * Dialog.KeywordMenu.CanonicalScaleY,
        SlotWidthVga * Dialog.KeywordMenu.CanonicalScaleX,
        SlotHeightVga * Dialog.KeywordMenu.CanonicalScaleY);
}
