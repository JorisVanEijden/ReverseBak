namespace BetrayalAtKrondor.Tests.Spell;

using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// Where the caster's highlight ring goes on the cast screen's portrait row.
/// </summary>
public class CastCasterHighlightTests {
    [Fact]
    public void TheThreeSlotsTileTheBandTheOriginalSavesAndRestores() {
        // cga_save_rect_to_buffer(pDest, 0xf, 0x8e, 0xa8, 0x2d) — VGA (15,142) 168x45. That 168 is
        // three slots of 56 with nothing left over, which is what says the row is regular.
        (int x0, int y0, int w, int h) = CastCasterHighlight.SlotRect(0);
        (int x1, _, _, _) = CastCasterHighlight.SlotRect(1);
        (int x2, _, _, _) = CastCasterHighlight.SlotRect(2);

        Assert.Equal(15 * 5, x0);
        Assert.Equal(142 * 6, y0);
        Assert.Equal(56 * 5, w);
        Assert.Equal(45 * 6, h);

        // Evenly spaced, and the three together are exactly the saved band.
        Assert.Equal(x1 - x0, x2 - x1);
        Assert.Equal(168 * 5, (x2 + w) - x0);
    }

    [Fact]
    public void SlotOneSitsWhereTheORIGINALSInkIs_NotWhereTheClickAreaIs() {
        // *** THE CASE THAT PICKS BETWEEN THE TWO CANDIDATE ORIGINS. *** REQ_CAST's party click
        // areas are at canonical x 70 / 365 / 660, which is tempting because the portraits are drawn
        // inside them. They are not the highlight's row: measured on the original's render, the ring
        // ink around the middle portrait starts at canonical x 360. The sprite carries a one-pixel
        // transparent margin, so an origin of 355 puts ink at 360 and an origin of 365 puts it at
        // 370. The band model predicts what was measured; the click-area model is 10 out here and 25
        // out at the last slot, because the click areas are not evenly spaced and this row is.
        Assert.Equal(355, CastCasterHighlight.SlotRect(1).X);
        Assert.Equal(635, CastCasterHighlight.SlotRect(2).X);
        Assert.NotEqual(365, CastCasterHighlight.SlotRect(1).X);
    }

    [Fact]
    public void TheSpriteIsTheSEVENTHCastfaceEntry_TheOneAfterTheSixBeads() {
        // CASTFACE.BMX 0..5 are the ring beads (40x18 each); 6 is the 280x270 circle outline. Taking
        // the icon set from CastRingLayout rather than repeating the filename keeps the two from
        // drifting if the ring's asset ever moves.
        Assert.Equal(6, CastCasterHighlight.Icon);
        Assert.Equal($"{CastRingLayout.IconSet}#6", CastCasterHighlight.SpriteKey);
    }

    [Fact]
    public void TheSlotBoxIsExactlyTheSpriteSize_SoItIsNeverRescaled() {
        // The extracted CASTFACE.BMX[6] is 280x270 canonical. If the box the renderer gives it were
        // any other size the circle would come out an ellipse, which is the failure that would look
        // like a palette or aspect bug rather than a layout one.
        (_, _, int w, int h) = CastCasterHighlight.SlotRect(0);
        Assert.Equal(280, w);
        Assert.Equal(270, h);
    }
}
