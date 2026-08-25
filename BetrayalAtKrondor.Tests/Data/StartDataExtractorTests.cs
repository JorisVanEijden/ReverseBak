namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Combat;
using GameData.Resources.Config;

using ResourceExtraction.Extractors;
using ResourceExtraction.Imaging;

using System.IO;

using Xunit;

/// <summary>
/// Verifies START.DAT parsing against the real 20-byte file. The format has no header, count or
/// terminator, so the field ORDER is the only thing holding the meaning — which makes a
/// verbatim-bytes test the right shape: a wrong order still parses and still produces ten plausible
/// numbers.
/// </summary>
public class StartDataExtractorTests {
    // START.DAT, verbatim.
    private static readonly byte[] Shipped = {
        0x00, 0x04, // combatCameraHeightAboveGround = 1024
        0x20, 0x03, // combatCameraHeightUnderground = 800
        0xc0, 0xf7, // combatCameraPitchAboveGround  = -2112
        0x2a, 0xf4, // combatCameraPitchUnderground  = -3030
        0x2c, 0x01, // combatGridCellSize      = 300
        0x0d, 0x00, // viewport x      = 13 VGA
        0x0b, 0x00, // viewport y      = 11 VGA
        0x26, 0x01, // viewport width  = 294 VGA
        0x65, 0x00, // viewport height = 101 VGA
        0x09, 0x00, // projectionShift = 9
    };

    private static StartData Extract() =>
        new StartDataExtractor().Extract("START.DAT", new MemoryStream(Shipped));

    [Fact]
    public void TheCombatCameraSitsLowerAndLooksSteeperUnderground() {
        StartData start = Extract();

        Assert.Equal(1024, start.CombatCameraHeightAboveGround);
        Assert.Equal(800, start.CombatCameraHeightUnderground);
        Assert.Equal(-2112, start.CombatCameraPitchAboveGround);
        Assert.Equal(-3030, start.CombatCameraPitchUnderground);

        // The relationship, not just the numbers: a dungeon is a tighter space, so the eye drops and
        // the view tips further down. A field-order slip that swapped the pairs would satisfy the
        // equalities above only by accident and would fail these.
        Assert.True(start.CombatCameraHeightUnderground < start.CombatCameraHeightAboveGround);
        Assert.True(start.CombatCameraPitchUnderground < start.CombatCameraPitchAboveGround);
    }

    [Fact]
    public void ThePitchesAreSignedAngles_NotLargePositiveOnes() {
        StartData start = Extract();

        // Read as u16 these become 63424 and 62506 — both plausible-looking angles that tilt the
        // camera the wrong way. Nothing else in the file is negative, so this is the one field pair
        // where the signedness is observable at all.
        Assert.True(start.CombatCameraPitchAboveGround < 0);
        Assert.True(start.CombatCameraPitchUnderground < 0);
    }

    [Fact]
    public void TheGridCellSizeIsWhatCentresTheArenaOnTheParty() {
        StartData start = Extract();

        Assert.Equal(300, start.CombatGridCellSize);

        // The engine offsets combat tile x by -1200 before rotating it by the camera heading. That
        // constant is not arbitrary: it is half the grid's total width, which is what puts the
        // arena's middle on the party's line of sight. Asserting the relationship rather than the
        // number means a different build's cell size would still have to satisfy it.
        const int LateralOffset = 1200;
        Assert.Equal(LateralOffset, start.CombatGridCellSize * CombatGrid.Width / 2);
    }

    [Fact]
    public void TheViewportCrossesIntoCanonicalSpace() {
        StartData start = Extract();

        // Stored as VGA pixels; the extractor is the boundary where the original's 320x200 stops.
        Assert.Equal(AspectCorrection.ScaleVgaX(13), start.ViewportX);
        Assert.Equal(AspectCorrection.ScaleVgaY(11), start.ViewportY);
        Assert.Equal(AspectCorrection.ScaleVgaX(294), start.ViewportWidth);
        Assert.Equal(AspectCorrection.ScaleVgaY(101), start.ViewportHeight);
    }

    [Fact]
    public void TheProjectionShiftIsUsableAsAShiftCount() {
        StartData start = Extract();

        Assert.Equal(9, start.ProjectionShift);

        // The renderer does `1 << value`. A negative or oversized exponent would be undefined there,
        // so the range is part of the field's meaning rather than a sanity check.
        Assert.InRange(start.ProjectionShift, 0, 15);
    }
}
