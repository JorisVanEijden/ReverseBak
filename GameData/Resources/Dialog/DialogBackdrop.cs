namespace GameData.Resources.Dialog;

/// <summary>
/// The full-screen parchment some dialogs are drawn on.
/// </summary>
/// <remarks>
/// <b>It is not the dialog TYPE that decides this.</b> The obvious reading — that
/// <see cref="DialogType.PlainFullScreen"/> means "full-screen parchment" — is wrong, and it is the
/// reading a port arrives at naturally, because the type is called *full screen* and its style row
/// really is a near-full-screen area. What the type actually selects is the text's area and pens
/// (<see cref="DialogStyleTable"/> row 6); the backdrop is chosen separately, by a FLAG, and the two
/// do not have to agree.
///
/// <para><c>ExecuteDialog</c> gates the whole backdrop block on
/// <c>dialogEntry.flags &amp; 2</c> (<c>test es:[bx+dialogEntry.flags], 2</c> at 0x49903) — i.e.
/// <see cref="DialogEntryFlags.IsolatePalette"/>. Inside it, <c>resourceLoadSCX("Dialog.scr")</c>
/// at 0x499dc reads the image into the offscreen buffer, and 0x499f9-0x49a08 blit it to the visible
/// buffer at (0,0) for the whole 320x200 — a screen-filling copy, not a panel.</para>
///
/// <para><b>Worked example, because this is easy to get backwards.</b> The "Someone was calling"
/// narrative (DIAL_Z30) is <see cref="DialogType.PlainFullScreen"/> and carries only
/// <c>Legacy10</c>, so it gets NO backdrop — its text is drawn over whatever is already on screen.
/// The questioning menu it leads to (dialog 2000001, DIAL_Z20) carries
/// <see cref="DialogEntryFlags.IsolatePalette"/>, so that one does get the parchment. A port that
/// keys the backdrop off the type would put it on the wrong one of the two.</para>
///
/// <para><c>Legacy10</c> (0x0010) is not a second backdrop flag: no read of
/// <c>dialogEntry.flags</c> anywhere in the dialog engine tests that bit, and the sites that pass
/// the flags word on wholesale reach either <c>test_flag_80_of_3rd_arg</c> (dead stubs) or the
/// input wait. It is set in the shipped data and examined by nothing that draws.</para>
/// </remarks>
public static class DialogBackdrop {
    /// <summary>
    /// The parchment image — <c>Dialog.scr</c> in the original's call, which is the same archive
    /// member our locator reaches as <c>DIALOG.SCX</c>.
    /// </summary>
    /// <remarks>
    /// Verified by extracting it: a torn-edged brown parchment covering the full frame. It carries
    /// no text or chrome of its own, so everything else — the body text and the corner vines — is
    /// drawn on top.
    /// </remarks>
    public const string Resource = "DIALOG.SCX";

    /// <summary>The flag that decides it.</summary>
    public const DialogEntryFlags Flag = DialogEntryFlags.IsolatePalette;

    /// <summary>Whether this entry is drawn on the full-screen parchment.</summary>
    public static bool DrawsFullScreenBackdrop(DialogEntryFlags flags) => (flags & Flag) != 0;

    /// <summary>Whether this entry is drawn on the full-screen parchment.</summary>
    public static bool DrawsFullScreenBackdrop(DialogEntry entry) =>
        entry != null && DrawsFullScreenBackdrop(entry.Flags);

    /// <summary>
    /// <b>The world keeps drawing on top of the parchment.</b> The blit fills the screen, and then
    /// the world viewport is re-rendered over it, so the speaker stands against the landscape.
    /// </summary>
    /// <remarks>
    /// This is the part a port gets wrong by reading only the blit: the parchment covers everything
    /// for one instant and the viewport is immediately painted back. <c>ExecuteDialog</c>'s
    /// backdrop arm saves the world camera and widget, re-points the camera (see
    /// <see cref="RepointsCameraForSpeaker"/>), calls the world renderer, outlines the viewport and
    /// restores both. The image itself carries no green field — extracting it gives bare parchment.
    /// </remarks>
    public const bool RedrawsWorldViewport = true;

    /// <summary>
    /// <b>The camera is MOVED before that render, so the view is not what the player was looking
    /// at.</b>
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
    /// <c>bGfx_outline_color = 6</c> with fill and clipping both switched off, so the line lands
    /// OUTSIDE the viewport rather than eating a pixel of the world.
    /// </remarks>
    public const int ViewportOutlinePen = 6;

    /// <summary>
    /// How far outside the viewport that outline sits, in original screen px.
    /// </summary>
    /// <remarks>
    /// The call is <c>draw_rect_filled(xmin - 1, ymin - 1, (xmax - xmin) + 3, (ymax - ymin) + 3)</c>
    /// — one px out on every side, which is what makes it a frame around the world rather than a
    /// border drawn over its edge.
    /// </remarks>
    public const int ViewportOutlineInset = 1;
}
