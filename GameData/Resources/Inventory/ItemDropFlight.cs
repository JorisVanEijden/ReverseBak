namespace GameData.Resources.Inventory;

/// <summary>
/// The sprite flight an item makes when it is dropped onto a party portrait.
/// </summary>
/// <remarks>
/// <b><c>invui_animate_item_fly</c> (INVENTOR.C:553-600).</b> On a transfer that actually happened
/// (<c>xfer_result &gt; 0</c>) the item's own inventory icon is blitted fifteen times, shrinking,
/// between the cursor and the portrait it was dropped on. There is no frame wait in the loop — one
/// <c>screen_frame_present()</c> per step — so the whole thing lasts about 0.21 s at mode 13h's
/// ~70 Hz.
///
/// <para><b>This lives here, away from the screen, because the arithmetic has already been wrong
/// once.</b> Written the intuitive way — offset from the START and interpolate toward the end — the
/// flight runs backwards and lands the item on the cursor instead of the portrait. In the game that
/// is invisible whenever the drop is on the portrait's exact centre, because the two endpoints
/// coincide and both forms agree; it only shows on an off-centre drop. A live play-verify passed
/// against the broken version. Pure arithmetic in front of a test is the only thing that catches
/// it.</para>
/// </remarks>
public static class ItemDropFlight {
    /// <summary>
    /// Steps in the flight — the original's <c>for (i = 14; i &gt;= 0; i--)</c>, so fifteen.
    /// </summary>
    /// <remarks>
    /// Also the divisor: every term in the original is over <c>0xf</c>, so the count and the scale
    /// denominator are the same number and must stay so.
    /// </remarks>
    public const int Steps = 15;

    /// <summary>Cue for an item handed to a party member.</summary>
    /// <remarks><c>audio_sfx_play_n_times(is_shop != 0 ? 0x3c : 0x3d, 0, 0)</c>.</remarks>
    public const int SoundId = 0x3d;

    /// <summary>The cue when the item came off a shop's shelf instead.</summary>
    public const int ShopSoundId = 0x3c;

    /// <summary>The icon's width and height at step <paramref name="step"/>.</summary>
    /// <remarks>
    /// <c>(image-&gt;nWidth * i) / 0xf</c>. At the first drawn step (14) the icon is 14/15 of full
    /// size, and at the last (0) it is nothing — the item is absorbed rather than set down.
    /// </remarks>
    public static (double Width, double Height) SizeAt(double fullWidth, double fullHeight,
        int step) =>
        (fullWidth * step / Steps, fullHeight * step / Steps);

    /// <summary>
    /// The icon's top-left corner at <paramref name="step"/>, counting DOWN from
    /// <see cref="Steps"/> - 1 to 0.
    /// </summary>
    /// <param name="cursorX">Where the drag was released — the original's <c>dst</c>.</param>
    /// <param name="cursorY">Where the drag was released.</param>
    /// <param name="portraitX">The portrait's centre — the original's <c>src</c>.</param>
    /// <param name="portraitY">The portrait's centre.</param>
    /// <remarks>
    /// <b>The interpolation is anchored on the PORTRAIT, not on the cursor.</b> The original is
    /// <c>x = ((dst_x - src_x) * i / 0xf + src_x) - scaled_w / 2</c>, and INVENTOR.C:754 passes
    /// <c>hovered</c>'s centre as <c>src</c> and the cursor as <c>dst</c> — the reverse of how those
    /// two names read. So the moving term is <c>cursor - portrait</c> and it decays to zero, leaving
    /// the sprite on the portrait.
    ///
    /// <para>Half the SCALED size comes off, not half the full size, which is what keeps the
    /// shrinking icon centred on its path instead of drifting up and left as it gets smaller.</para>
    /// </remarks>
    public static (double Left, double Top) CornerAt(
        double cursorX, double cursorY, double portraitX, double portraitY,
        double fullWidth, double fullHeight, int step) {
        (double w, double h) = SizeAt(fullWidth, fullHeight, step);
        return ((cursorX - portraitX) * step / Steps + portraitX - w / 2,
            (cursorY - portraitY) * step / Steps + portraitY - h / 2);
    }
}
