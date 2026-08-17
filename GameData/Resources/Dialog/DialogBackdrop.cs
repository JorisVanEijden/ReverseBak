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
}
