namespace GameData.Resources.Dialog;

/// <summary>
/// The full-screen parchment some dialogs are drawn on.
/// </summary>
/// <remarks>
/// <b>There are TWO triggers, not one</b>, and they sit on opposite sides of the same branch in
/// <c>ExecuteDialog</c>. Reading either one alone gives a rule that is right about half the
/// dialogs in the game and confidently wrong about the other half.
///
/// <para><c>ExecuteDialog</c> tests <c>dialogEntry.flags &amp; 2</c>
/// (<see cref="DialogEntryFlags.IsolatePalette"/>) at 0x49903:</para>
/// <list type="bullet">
///   <item><b>Flag SET</b> → 0x4990e. Saves the palette, loads the parchment into the offscreen
///     buffer (<c>resourceLoadSCX("Dialog.scr")</c>, 0x499dc), blits it over the whole 320x200,
///     then <b>re-renders the world viewport on top of it</b> from a re-pointed camera
///     (see <see cref="DialogBackdropCamera"/>). This is the speaker-against-the-landscape look.</item>
///   <item><b>Flag CLEAR</b> → 0x49b21, which is <i>not</i> a dead end. It tests the entry's
///     <see cref="DialogEntry.DialogType"/>:
///     <c>mov al, [bx+dialogType]; dec ax; cmp ax, 5; jnz</c> — i.e. <c>dialogType == 6</c>,
///     <see cref="DialogType.PlainFullScreen"/>. If it matches, the same parchment is loaded and
///     blitted full-screen (0x49b3b-0x49b6f) and <b>nothing is drawn back over it</b>.</item>
/// </list>
///
/// <para><b>Worked example — this file previously stated the opposite.</b> The "Someone was
/// calling" narrative (DIAL_Z30 @120988) is <see cref="DialogType.PlainFullScreen"/> carrying only
/// <c>Legacy10</c>. The earlier note concluded it therefore gets NO backdrop and its text is drawn
/// over whatever is on screen. That is exactly what our port did, and it is exactly what the
/// original does not do: type 6 is the flagless trigger, so this entry gets the parchment — which
/// is what a player sees when they meet squire Phillip. The lesson is that a `jnz` past one test
/// is not proof of absence; the branch it lands on has to be read too.</para>
///
/// <para><c>Legacy10</c> (0x0010) remains a non-participant: no read of <c>dialogEntry.flags</c>
/// anywhere in the dialog engine tests that bit. It is set in the shipped data and examined by
/// nothing that draws.</para>
///
/// <para><b>Why the panel does not paint its own fill.</b> Style row 6 has
/// <c>FillPenColor == 0</c>, and <c>dialog_DrawChrome</c> reads that as "blit the dialog's
/// rectangle out of the offscreen buffer" (0x48735), not as "no fill" — which is why the row's
/// three chrome pens being zero does not mean the panel is transparent. The parchment reaches the
/// panel through the buffer, so in our port it is drawn as a backdrop behind the panel rather than
/// as a fill on it.</para>
/// </remarks>
public static class DialogBackdrop {
    /// <summary>
    /// The parchment image — <c>Dialog.scr</c> in the original's call, which is the same archive
    /// member our locator reaches as <c>DIALOG.SCX</c>. <c>resourceLoadSCX</c> rewrites the final
    /// character of the name to <c>'x'</c> before opening it, so the two spellings are one file.
    /// </summary>
    /// <remarks>
    /// Verified by extracting it: a torn-edged brown parchment covering the full frame. It carries
    /// no text, no chrome and <b>no vines</b> of its own — the corner vines are separate sprites
    /// (<see cref="DialogVineCorners"/>) and the body text is drawn on top.
    /// </remarks>
    public const string Resource = "DIALOG.SCX";

    /// <summary>The flag that triggers the backdrop-with-world-viewport form.</summary>
    public const DialogEntryFlags Flag = DialogEntryFlags.IsolatePalette;

    /// <summary>The dialog type that triggers the backdrop on its own, with no flag.</summary>
    public const DialogType FullScreenType = DialogType.PlainFullScreen;

    /// <summary>Whether this entry is drawn on the full-screen parchment.</summary>
    public static bool DrawsFullScreenBackdrop(DialogEntryFlags flags, DialogType type) =>
        (flags & Flag) != 0 || type == FullScreenType;

    /// <summary>Whether this entry is drawn on the full-screen parchment.</summary>
    public static bool DrawsFullScreenBackdrop(DialogEntry entry) =>
        entry != null && DrawsFullScreenBackdrop(entry.Flags, entry.DialogType);

    /// <summary>
    /// <b>The world keeps drawing on top of the parchment — but only on the FLAG path.</b>
    /// </summary>
    /// <remarks>
    /// The flagged form blits the parchment, then immediately re-renders the world viewport over
    /// it, so the speaker stands against the landscape and the parchment survives only as a border
    /// around the view. The type-6 form does not: after its blit at 0x49b6f control falls straight
    /// through to the text, leaving the parchment covering the screen. A port that re-renders the
    /// world for both would punch a hole in the narrative panel the player is meant to read.
    /// </remarks>
    public static bool RedrawsWorldViewport(DialogEntryFlags flags) => (flags & Flag) != 0;

    /// <summary>
    /// <b>The camera is MOVED before that render, so the view is not what the player was looking
    /// at.</b> Applies to the flag path only, for the same reason as
    /// <see cref="RedrawsWorldViewport"/>.
    /// </summary>
    /// <remarks>
    /// The original overwrites the camera's height with the zone default plus the world-crossing
    /// coordinate, its pitch with the zone default, and — when the speaker is a party member —
    /// turns the yaw roughly about-face and offsets it by the speaker's slot in the marching order,
    /// so each companion is framed against a slightly different bearing. It restores the saved
    /// camera immediately afterwards, so nothing the player sees afterwards moves.
    ///
    /// <para>That is why a reference screenshot of this dialog can show a flat, featureless green
    /// field while the party is standing beside a corpse on a road: the shot is of a DIFFERENT
    /// camera. Matching the original here means re-pointing the camera, not painting a fill.</para>
    ///
    /// <para><b>Not yet applied</b> — our backdrop renders the live camera. Tracked on the task; the
    /// rule is recorded here so the reason the two differ is not rediscovered from scratch.</para>
    /// </remarks>
    public const bool RepointsCameraForSpeaker = true;

    /// <summary>Palette pen the viewport's outline is drawn in.</summary>
    /// <remarks>
    /// <c>pen_color = 6</c> with fill and clipping both switched off, so the line lands
    /// OUTSIDE the viewport rather than eating a pixel of the world.
    /// </remarks>
    public const int ViewportOutlinePen = 6;

    /// <summary>
    /// How far outside the viewport that outline sits, in original screen px.
    /// </summary>
    /// <remarks>
    /// The call is <c>draw_box_or_border(xmin - 1, ymin - 1, (xmax - xmin) + 3, (ymax - ymin) + 3)</c>
    /// — one px out on every side, which is what makes it a frame around the world rather than a
    /// border drawn over its edge.
    /// </remarks>
    public const int ViewportOutlineInset = 1;
}
