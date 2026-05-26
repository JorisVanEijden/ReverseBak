namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using Xunit;

/// <summary>
/// Faithfulness tests for the C# port of <c>GetDialogTypeData</c> (KRONDOR.EXE
/// at 0x4856c). Each case is justified directly from the disassembly so that a
/// future change to the resolver fails loudly.
/// </summary>
public class DialogTypeResolverTests {

    // ────────────────── Precedence: no overrides ──────────────────

    [Fact]
    public void NoOverrides_NoActor_SourceByteZero_ReturnsDefaultRow2() {
        // mov dx, 2 at 0x4856f. Source byte 0 leaves the default in place
        // (the source-byte check at 0x4859f only overrides when nonzero).
        int id = DialogTypeResolver.ResolveEffectiveStyleId(
            DialogContext.None, sourceDialogType: 0, actorNumber: 0);

        Assert.Equal(2, id);
    }

    // ────────────────── Single-axis overrides ──────────────────

    [Fact]
    public void InGameContext_NoActor_SourceByteZero_ReturnsRow5() {
        var ctx = new DialogContext(InGameContextActive: true,
            FullScreenFlag1Active: false, FullScreenFlag2Active: false);

        int id = DialogTypeResolver.ResolveEffectiveStyleId(ctx, 0, 0);

        Assert.Equal(5, id);
    }

    [Fact]
    public void FullScreenFlag1_NoActor_SourceByteZero_ReturnsRow6() {
        var ctx = new DialogContext(false, FullScreenFlag1Active: true, false);

        int id = DialogTypeResolver.ResolveEffectiveStyleId(ctx, 0, 0);

        Assert.Equal(6, id);
    }

    [Fact]
    public void FullScreenFlag2_NoActor_SourceByteZero_ReturnsRow6() {
        // Either full-screen flag alone is sufficient (cmp byte_dseg_FBC, 0
        // at 0x48585; jnz to dx=6 path).
        var ctx = new DialogContext(false, false, FullScreenFlag2Active: true);

        int id = DialogTypeResolver.ResolveEffectiveStyleId(ctx, 0, 0);

        Assert.Equal(6, id);
    }

    [Fact]
    public void Actor_NoContext_SourceByteZero_ReturnsRow1() {
        // cmp es:[bx+dialogEntry.actorNr], 0 at 0x48592 → mov dx, 1.
        int id = DialogTypeResolver.ResolveEffectiveStyleId(
            DialogContext.None, 0, actorNumber: 17);

        Assert.Equal(1, id);
    }

    [Theory]
    [InlineData((byte)1)]
    [InlineData((byte)3)]
    [InlineData((byte)5)]
    [InlineData((byte)6)]
    public void SourceByte_NoContext_NoActor_ReturnsSourceByte(byte sourceByte) {
        // cmp dialogType, 0 at 0x4859f → mov dx, ax.
        int id = DialogTypeResolver.ResolveEffectiveStyleId(
            DialogContext.None, sourceByte, 0);

        Assert.Equal(sourceByte, id);
    }

    // ────────────────── Cross-axis precedence ──────────────────

    [Fact]
    public void InGameContext_BlocksFullScreenOverride() {
        // The `jmp short loc_ovr143_4AF` at 0x4857c skips the full-screen
        // branch entirely once dx=5 is set — so even with both flags asserted,
        // dx stays 5 (until actor/source byte overrides).
        var ctx = new DialogContext(InGameContextActive: true,
            FullScreenFlag1Active: true, FullScreenFlag2Active: true);

        int id = DialogTypeResolver.ResolveEffectiveStyleId(ctx, 0, 0);

        Assert.Equal(5, id);
    }

    [Fact]
    public void Actor_OverridesInGameContext() {
        // The actor check at 0x48592 runs unconditionally after the context
        // branch (control merges at loc_4AF). Actor = nonzero → dx = 1.
        var ctx = new DialogContext(InGameContextActive: true, false, false);

        int id = DialogTypeResolver.ResolveEffectiveStyleId(ctx, 0, actorNumber: 1);

        Assert.Equal(1, id);
    }

    [Fact]
    public void Actor_OverridesFullScreenContext() {
        var ctx = new DialogContext(false, true, false);

        int id = DialogTypeResolver.ResolveEffectiveStyleId(ctx, 0, actorNumber: 1);

        Assert.Equal(1, id);
    }

    [Fact]
    public void SourceByte_OverridesActor() {
        // The source-byte check at 0x4859f runs after the actor check; if
        // both fire, the source byte wins (mov dx, ax overwrites).
        int id = DialogTypeResolver.ResolveEffectiveStyleId(
            DialogContext.None, sourceDialogType: 3, actorNumber: 1);

        Assert.Equal(3, id);
    }

    [Fact]
    public void SourceByte_OverridesEverything() {
        var ctx = new DialogContext(true, true, true);

        int id = DialogTypeResolver.ResolveEffectiveStyleId(ctx, sourceDialogType: 6, actorNumber: 9);

        Assert.Equal(6, id);
    }

    // ────────────────── DialogEntry overload ──────────────────

    [Fact]
    public void EntryOverload_DoesNotMutateEntry() {
        // The original game writes back to dialogEntry.dialogType at 0x485af,
        // but the C# port treats the entry as immutable input.
        var entry = new DialogEntry {
            DialogType = DialogType.PlainWithoutBox, // 3
            ActorNumber = 0,
        };

        int id = DialogTypeResolver.ResolveEffectiveStyleId(DialogContext.None, entry);

        Assert.Equal(3, id);
        Assert.Equal(DialogType.PlainWithoutBox, entry.DialogType);
        Assert.Equal(0, entry.ActorNumber);
    }

    [Fact]
    public void ResolveStyle_PicksTheCorrectRow() {
        // Source = Normal (0), no overrides → effective 2 → row 2's area.
        // VGA (13, 11, 294, 101) → (4.0625, 5.5, 91.875, 50.5) percent.
        var entry = new DialogEntry { DialogType = DialogType.Normal, ActorNumber = 0 };

        DialogStyle style = DialogTypeResolver.ResolveStyle(DialogContext.None, entry);

        Assert.Equal(new DialogArea(4.0625f, 5.5f, 91.875f, 50.5f), style.DefaultArea);
    }

    // ────────────────── Style table integrity ──────────────────

    [Fact]
    public void StyleTable_Row0_IsUnreachableAndThrows() {
        // The dispatcher never produces dx=0 (default is 2, source byte 0
        // doesn't override). Row 0's bytes at 0x3a831 are init padding.
        Assert.False(DialogStyleTable.IsDefined(0));
        Assert.Throws<InvalidOperationException>(() => DialogStyleTable.Get(0));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)] // unreachable but defined as a faithful copy of row 1's bytes
    [InlineData(5)]
    [InlineData(6)]
    public void StyleTable_DefinedRows_AreLookable(int rowId) {
        Assert.True(DialogStyleTable.IsDefined(rowId));
        DialogStyle style = DialogStyleTable.Get(rowId);

        // Smoke check — defined rows must have a non-empty area.
        Assert.True(style.DefaultArea.WidthPct > 0f);
        Assert.True(style.DefaultArea.HeightPct > 0f);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    [InlineData(100)]
    public void StyleTable_OutOfRange_Throws(int rowId) {
        Assert.False(DialogStyleTable.IsDefined(rowId));
        Assert.Throws<ArgumentOutOfRangeException>(() => DialogStyleTable.Get(rowId));
    }

    [Fact]
    public void StyleTable_Row6_HasFullScreenArea() {
        // From bytes at 0x3a8b5: actionData VGA (25, 21, 270, 160)
        // → percent (7.8125, 10.5, 84.375, 80).
        DialogStyle style = DialogStyleTable.Get(6);

        Assert.Equal(new DialogArea(7.8125f, 10.5f, 84.375f, 80f), style.DefaultArea);
    }

    [Fact]
    public void StyleTable_Row3_HasNarrativeArea() {
        // PlainWithoutBox (cutscene narrative strip). actionData VGA
        // (8, 118, 305, 73) → percent (2.5, 59, 95.3125, 36.5). X is 8 (same as
        // row 1), NOT 16 — a prior transcription used 5% and pushed
        // LeftPct+WidthPct past 100%. Fields are (X, Y, Width, Height), per the
        // field use in dialog_DrawChrome (0x48632).
        DialogStyle style = DialogStyleTable.Get(3);

        Assert.Equal(new DialogArea(2.5f, 59f, 95.3125f, 36.5f), style.DefaultArea);
        Assert.True(style.DefaultArea.LeftPct + style.DefaultArea.WidthPct <= 100f);
    }

    [Fact]
    public void StyleTable_Row2_HasShadowAndBorder() {
        // From bytes at 0x3a859: field_4=0x01 (border), field_5=0x04 (shadow).
        DialogStyle style = DialogStyleTable.Get(2);

        Assert.True(style.HasBorder);
        Assert.Equal(0x01, style.BorderPenColor);
        Assert.True(style.HasDropShadow);
        Assert.Equal(0x04, style.ShadowPenColor);
    }

    [Fact]
    public void StyleTable_Rows_1_3_6_HaveNoChrome() {
        // Per the bytes at 0x3a831: rows 1, 3, and 6 all have field_1=field_4=field_5=0,
        // i.e. they preserve the underlying buffer (no fill, no border, no shadow).
        // The renderer's buffer-C→1 blit branch at 0x48735 handles them.
        foreach (int rowId in new[] { 1, 3, 6 }) {
            DialogStyle style = DialogStyleTable.Get(rowId);
            Assert.False(style.UsesTexturedFill);
            Assert.False(style.HasBorder);
            Assert.False(style.HasDropShadow);
        }
    }

    // ────────────────── Body text pen + text shadow ──────────────────
    // RenderDialogText (0x48d7b) draws body text in dialogTypeData.field_2
    // (every caller passes textColor = -1, so field_2 is always used, 0x490ff),
    // with a back-shadow in pen field_3 - 1 only when field_3 != 0 (0x490bf).
    // These are distinct from the chrome bevel pen (field_5 = ShadowPenColor).

    [Fact]
    public void StyleTable_Row1_ColoredWithoutBox_BodyPen10_ShadowPen1() {
        // From bytes at 0x3a845: field_2=0x0A (body text = bright cream pen 10,
        // same pen the name bubble uses), field_3=0x02 → text shadow in pen 1.
        DialogStyle style = DialogStyleTable.Get(1);

        Assert.Equal(0x0A, style.BodyTextPenColor);
        Assert.True(style.HasTextShadow);
        Assert.Equal(0x01, style.TextShadowPenColor);
    }

    [Fact]
    public void StyleTable_Row4_UnreachableTwinOfRow1() {
        // Row 4 is identical to row 1 (bytes at 0x3a881): same body/shadow pens.
        DialogStyle style = DialogStyleTable.Get(4);

        Assert.Equal(0x0A, style.BodyTextPenColor);
        Assert.True(style.HasTextShadow);
        Assert.Equal(0x01, style.TextShadowPenColor);
    }

    [Fact]
    public void StyleTable_Rows_2_3_5_6_BodyPen0_NoTextShadow() {
        // Bytes at 0x3a859 / 0x3a86d / 0x3a895 / 0x3a8a9: field_2=0 (body text
        // = pen 0, black in every BaK palette) and field_3=0 (no text shadow).
        // PlainWithoutBox (row 3, cutscene narrative) is therefore black text,
        // confirmed against the original game.
        foreach (int rowId in new[] { 2, 3, 5, 6 }) {
            DialogStyle style = DialogStyleTable.Get(rowId);
            Assert.Equal(0x00, style.BodyTextPenColor);
            Assert.False(style.HasTextShadow);
        }
    }

    [Fact]
    public void StyleTable_TextShadow_IsDistinctFromChromeBevel() {
        // Row 2 (Normal) has a chrome bevel (field_5=4 → HasDropShadow) but NO
        // text shadow (field_3=0). The two must not be conflated — the text
        // renderer keys off field_3, the chrome renderer off field_5.
        DialogStyle row2 = DialogStyleTable.Get(2);
        Assert.True(row2.HasDropShadow);   // chrome bevel
        Assert.False(row2.HasTextShadow);  // text shadow

        // Row 1 (ColoredWithoutBox) is the inverse: text shadow but no bevel.
        DialogStyle row1 = DialogStyleTable.Get(1);
        Assert.False(row1.HasDropShadow);
        Assert.True(row1.HasTextShadow);
    }
}
