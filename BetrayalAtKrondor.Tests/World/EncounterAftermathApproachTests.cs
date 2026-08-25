namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// Which side of the encounter's box the party finished on — the selector in front of the four
/// landings (<c>worldmove_aabb_outcode_rotated</c>).
/// </summary>
public class EncounterAftermathApproachTests {
    // A box occupying cells x 10..30, y 5..20 of one tile. Deliberately NOT square and NOT centred,
    // so a swapped axis or a swapped bound cannot pass by symmetry.
    private const int StartX = 10;
    private const int EndY = 20;
    private const int EndX = 30;
    private const int StartY = 5;

    private const int TileX = 3;
    private const int TileY = 7;

    private static int At(int cellX, int cellY) =>
        EncounterAftermath.ApproachDirection(TileX, TileY,
            WorldPlacement.CornerOf(TileX, cellX), WorldPlacement.CornerOf(TileY, cellY),
            StartX, EndY, EndX, StartY);

    [Fact]
    public void EachSideOfTheBoxGetsItsOwnAnswer() {
        Assert.Equal(1, At(20, 25));   // past the max-Y edge
        Assert.Equal(2, At(5, 12));    // short of the min-X edge
        Assert.Equal(4, At(20, 2));    // short of the min-Y edge
        Assert.Equal(8, At(40, 12));   // the +X side
    }

    [Fact]
    public void TheYTestComesFirst_SoACornerIsNotWhicheverAxisYouCheckedLast() {
        // Beyond max-Y AND left of min-X. The original tests Y first, so this is 1 and not 2 —
        // an implementation that ordered the comparisons differently would answer 2 here and send
        // the party to a different landing.
        Assert.Equal(1, At(0, 25));

        // Short of min-X AND short of min-Y: X is tested before the second Y, so 2 wins over 4.
        Assert.Equal(2, At(0, 0));
    }

    [Fact]
    public void StandingInsideTheBoxAnswersEight() {
        // Not a degenerate case — after a fight the party is usually still on the encounter's
        // ground, so this is the COMMON answer rather than an edge one.
        Assert.Equal(8, At(20, 12));
    }

    [Fact]
    public void TheTwoMinEdgesAreTestedOneCellIn_TheMaxEdgeIsNot() {
        // min-X: standing exactly on column 10 still counts as "short of" it, because the
        // comparison is against column 11.
        Assert.Equal(2, At(StartX, 12));
        Assert.NotEqual(2, At(StartX + 1, 12));

        // min-Y: same shape, against row 6.
        Assert.Equal(4, At(20, StartY));
        Assert.NotEqual(4, At(20, StartY + 1));

        // max-Y: no +1, so standing exactly on row 20 is NOT past it.
        Assert.NotEqual(1, At(20, EndY));
        Assert.Equal(1, At(20, EndY + 1));
    }

    [Fact]
    public void TheBoxIsReadInItsOnDiskOrder_NotAsMinMinMaxMax() {
        // Handing the same four bytes across as a conventional (minX, minY, maxX, maxY) rectangle
        // swaps the two Y bounds. The party here stands INSIDE the box, at row 12 between bounds 5
        // and 20 — the case where the swap is visible: the "past max-Y" test then compares against
        // row 5 rather than row 20, row 12 is past that, and a party standing on the encounter's
        // own ground is reported as having left it to the far side.
        int correct = At(20, 12);
        int swapped = EncounterAftermath.ApproachDirection(TileX, TileY,
            WorldPlacement.CornerOf(TileX, 20), WorldPlacement.CornerOf(TileY, 12),
            StartX, StartY, EndX, EndY);

        Assert.Equal(8, correct);
        Assert.Equal(1, swapped);
        Assert.NotEqual(correct, swapped);
    }

    [Fact]
    public void TheTriggerOverloadPassesTheBytesInTheOrderThatCannotBeGotWrong() {
        var trigger = new TileEventTrigger {
            StartX = StartX, EndY = EndY, EndX = EndX, StartY = StartY,
        };

        foreach ((int cellX, int cellY) in new[] { (20, 25), (5, 12), (20, 2), (40, 12), (20, 12) }) {
            Assert.Equal(At(cellX, cellY),
                EncounterAftermath.ApproachDirection(trigger, TileX, TileY,
                    WorldPlacement.CornerOf(TileX, cellX), WorldPlacement.CornerOf(TileY, cellY)));
        }
    }

    [Fact]
    public void EveryAnswerSelectsALandingAndOnlyThreeAreDistinct() {
        Assert.Equal(EncounterAftermath.Landing.Direction1, EncounterAftermath.LandingFor(At(20, 25)));
        Assert.Equal(EncounterAftermath.Landing.Direction2, EncounterAftermath.LandingFor(At(5, 12)));
        Assert.Equal(EncounterAftermath.Landing.Direction4, EncounterAftermath.LandingFor(At(20, 2)));
        Assert.Equal(EncounterAftermath.Landing.Direction8, EncounterAftermath.LandingFor(At(40, 12)));
    }

    [Fact]
    public void ANullTriggerAnswersTheDefaultLandingRatherThanThrowing() {
        Assert.Equal(EncounterAftermath.Landing.Direction1,
            EncounterAftermath.LandingFor(EncounterAftermath.ApproachDirection(null, 0, 0, 0, 0)));
    }
}
