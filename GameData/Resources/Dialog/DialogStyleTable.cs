namespace GameData.Resources.Dialog;

/// <summary>
/// Verbatim port of the 7-row × 20-byte <c>dialogTypeData</c> table at
/// 0x3a831 in KRONDOR.EXE. Indexed by the value the dispatcher
/// (<c>GetDialogTypeData</c> at 0x4856c) writes back to
/// <c>dialogEntry.dialogType</c> — i.e. the *effective* style id, which is
/// not necessarily the source byte from the DDX file.
///
/// Each row's pen fields come straight from the raw table bytes:
/// <c>field_1</c>=Fill, <c>field_2</c>=BodyText, <c>field_3</c>=TextShadow
/// source, <c>field_4</c>=Border, <c>field_5</c>=chrome bevel. See
/// <see cref="DialogStyle"/> for how each field is consumed.
/// </summary>
public static class DialogStyleTable {
    /// <summary>Number of rows in the original table.</summary>
    public const int Length = 7;

    private static readonly DialogStyle?[] Rows = {
        // Row 0: unused. The dispatcher never returns 0 (default is 2; source
        // byte == 0 leaves the default in place — it does not override to 0).
        // The bytes at 0x3a831 for this row are init padding.
        null,

        // Row 1: source DialogType.ColoredWithoutBox, OR forced when actorNr != 0.
        // Raw bytes (0x3a845): field_2=0x0A → body text uses palette pen 10
        // (the bright cream/gold of the typical BaK cutscene palette, the same
        // pen the name bubble draws with); field_3=0x02 → text drop-shadow in
        // pen 1 (field_3-1). No chrome (fill/border/bevel all 0).
        // VGA (8, 120, 305, 75) → percent below. Text inset field_9=field_A=8
        // VGA px over a 305-px-wide panel → 8/305 = 2.623 %.
        new DialogStyle(
            FillPenColor: 0x00,
            BorderPenColor: 0x00,
            ShadowPenColor: 0x00,
            BodyTextPenColor: 0x0A,
            TextShadowPenSource: 0x02,
            DefaultArea: new DialogArea(2.5f, 60f, 95.3125f, 37.5f),
            TextPadLeftPct: 2.62295f,
            TextPadRightPct: 2.62295f),

        // Row 2: the *default fallback*. Dispatcher returns 2 when no
        // override fires AND source byte is 0 — i.e. this is the actual
        // chrome for DialogType.Normal in the source data.
        // Raw bytes (0x3a859): field_2=0 → body text in pen 0 (black);
        // field_3=0 → no text shadow. Chrome: field_1=1 (stripe fill),
        // field_4=1 (border), field_5=4 (bevel).
        // VGA (13, 11, 294, 101) → percent below. Text inset field_9=field_A=10
        // VGA px over a 294-px-wide panel → 10/294 = 3.401 %.
        new DialogStyle(
            FillPenColor: 0x01,
            BorderPenColor: 0x01,
            ShadowPenColor: 0x04,
            BodyTextPenColor: 0x00,
            TextShadowPenSource: 0x00,
            DefaultArea: new DialogArea(4.0625f, 5.5f, 91.875f, 50.5f),
            TextPadLeftPct: 3.40136f,
            TextPadRightPct: 3.40136f),

        // Row 3: source DialogType.PlainWithoutBox (cutscene narrative strip).
        // Raw bytes (0x3a86d): field_2=0 → body text in pen 0 (black);
        // field_3=0 → no text shadow; no chrome. The bare wooden strip in the
        // cutscene buffer supplies all the contrast.
        // VGA (8, 118, 305, 73) → percent below. X=8 → 8/320 = 2.5% (same as
        // row 1); a prior transcription had 5% here, which made LeftPct+WidthPct
        // overflow 100%.
        // Text inset field_9=field_A=8 VGA px over a 305-px-wide panel →
        // 8/305 = 2.623 %.
        new DialogStyle(
            FillPenColor: 0x00,
            BorderPenColor: 0x00,
            ShadowPenColor: 0x00,
            BodyTextPenColor: 0x00,
            TextShadowPenSource: 0x00,
            DefaultArea: new DialogArea(2.5f, 59f, 95.3125f, 36.5f),
            TextPadLeftPct: 2.62295f,
            TextPadRightPct: 2.62295f),

        // Row 4: unreachable. Identical to row 1; no source byte produces 4
        // and the dispatcher never assigns dx=4.
        // VGA (8, 120, 305, 75) → percent below. Text inset as row 1.
        new DialogStyle(
            FillPenColor: 0x00,
            BorderPenColor: 0x00,
            ShadowPenColor: 0x00,
            BodyTextPenColor: 0x0A,
            TextShadowPenSource: 0x02,
            DefaultArea: new DialogArea(2.5f, 60f, 95.3125f, 37.5f),
            TextPadLeftPct: 2.62295f,
            TextPadRightPct: 2.62295f),

        // Row 5: source DialogType.NormalInGame, OR forced by the in-game
        // context flag (dialog_word_3AC96 != 0). Identical pens to row 2.
        // Raw bytes (0x3a895): field_2=0 → body text pen 0; field_3=0 → no
        // text shadow; field_1=1, field_4=1, field_5=4 chrome.
        // VGA (13, 11, 294, 121) → percent below. Text inset as row 2
        // (field_9=field_A=10 over 294 → 3.401 %).
        new DialogStyle(
            FillPenColor: 0x01,
            BorderPenColor: 0x01,
            ShadowPenColor: 0x04,
            BodyTextPenColor: 0x00,
            TextShadowPenSource: 0x00,
            DefaultArea: new DialogArea(4.0625f, 5.5f, 91.875f, 60.5f),
            TextPadLeftPct: 3.40136f,
            TextPadRightPct: 3.40136f),

        // Row 6: source DialogType.PlainFullScreen, OR forced by either
        // full-screen flag (bool_word_dseg_C08, byte_dseg_FBC). The renderer
        // also draws corner-vine sprites for this row (via the
        // `dec ax; cmp ax, 5` branch at 0x4886c, which fires when
        // dialogType == 6). Raw bytes (0x3a8a9): field_2=0 → body text pen 0;
        // field_3=0 → no text shadow; no chrome pens.
        // VGA (25, 21, 270, 160) → percent below. Text inset field_9=field_A=1
        // VGA px over a 270-px-wide panel → 1/270 = 0.370 %.
        new DialogStyle(
            FillPenColor: 0x00,
            BorderPenColor: 0x00,
            ShadowPenColor: 0x00,
            BodyTextPenColor: 0x00,
            TextShadowPenSource: 0x00,
            DefaultArea: new DialogArea(7.8125f, 10.5f, 84.375f, 80f),
            TextPadLeftPct: 0.37037f,
            TextPadRightPct: 0.37037f),
    };

    /// <summary>
    /// Look up the style for an effective row index (the value the dispatcher
    /// returns and writes back to <c>dialogEntry.dialogType</c>).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="rowIndex"/> is outside 1..6.</exception>
    /// <exception cref="InvalidOperationException">If row 0 is requested (dispatcher never produces it).</exception>
    public static DialogStyle Get(int rowIndex) {
        if (rowIndex < 0 || rowIndex >= Length) {
            throw new ArgumentOutOfRangeException(nameof(rowIndex), rowIndex, $"Must be 0..{Length - 1}.");
        }
        DialogStyle? style = Rows[rowIndex];
        if (style is null) {
            throw new InvalidOperationException(
                $"Row {rowIndex} of dialogTypeData is unused in the original game (dispatcher never returns this index).");
        }
        return style.Value;
    }

    /// <summary>True if the given index has a defined style (not unreachable padding).</summary>
    public static bool IsDefined(int rowIndex) => rowIndex >= 0 && rowIndex < Length && Rows[rowIndex] is not null;
}
