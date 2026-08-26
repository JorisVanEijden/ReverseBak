namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// Where a combatant stands for a grid cell.
/// </summary>
/// <remarks>
/// The trap these pin down: the arena has TWO cell-to-world formulas that differ by half a cell in
/// one axis, and the wrong one is right there in <see cref="CombatGroundCheck"/> already being used
/// for the ground sweep.
/// </remarks>
public class CombatArenaPlacementTests {
    private const int CellSize = 300; // the shipped StartData.CombatGridCellSize

    [Fact]
    public void SidewaysAgreesWithTheGroundSweep() {
        for (var column = 0; column < CombatGrid.Width; column++) {
            (int placeAcross, _) = CombatArenaPlacement.CellOffset(column, 0, CellSize);
            (int sweepAcross, _) = CombatGroundCheck.SampleOffset(column, 0, CellSize);
            Assert.Equal(sweepAcross, placeAcross);
        }
    }

    [Fact]
    public void ForwardDiffersFromTheGroundSweepByHalfACell() {
        // *** The failure this catches. *** Reusing SampleOffset to place a combatant puts every
        // actor half a cell too near the party — uniform enough to read as "the arena looks a bit
        // small" instead of as a wrong formula.
        for (var row = 0; row < CombatGrid.Height; row++) {
            (_, int placeAway) = CombatArenaPlacement.CellOffset(0, row, CellSize);
            (_, int sweepAway) = CombatGroundCheck.SampleOffset(0, row, CellSize);
            Assert.Equal(CombatArenaPlacement.ForwardDifferenceFromGroundSweep(CellSize),
                placeAway - sweepAway);
        }
    }

    [Fact]
    public void TheNearestRowStillSitsTheWholeForwardOffsetAway() {
        (_, int away) = CombatArenaPlacement.CellOffset(0, CombatArenaPlacement.NearRow, CellSize);
        Assert.Equal(CombatGroundCheck.ForwardOffset + (CellSize / 2), away);
    }

    [Fact]
    public void RowsIncreaseAWAYFromTheParty() {
        // A port that read row 0 as the far edge would stand the party behind the monsters.
        (_, int near) = CombatArenaPlacement.CellOffset(0, 0, CellSize);
        (_, int far) = CombatArenaPlacement.CellOffset(0, CombatGrid.Height - 1, CellSize);
        Assert.True(far > near);
    }

    [Fact]
    public void TheFootprintIsCentredOnTheLineOfSight() {
        (int left, _) = CombatArenaPlacement.CellOffset(0, 0, CellSize);
        (int right, _) = CombatArenaPlacement.CellOffset(CombatGrid.Width - 1, 0, CellSize);
        Assert.Equal(0, left + right); // symmetric about the party's forward axis
    }

    [Fact]
    public void ADifferentCellSizeMovesTheFootprintAndItsOccupantsTogether() {
        const int wider = CellSize * 2;
        (int placeAcross, _) = CombatArenaPlacement.CellOffset(0, 0, wider);
        (int sweepAcross, _) = CombatGroundCheck.SampleOffset(0, 0, wider);
        Assert.Equal(sweepAcross, placeAcross);
    }

    [Theory]
    [InlineData(0, 0, false, true)]
    [InlineData(0, CombatGrid.Height - 1, false, true)]
    [InlineData(0, CombatGrid.UndergroundPlayableRows, true, false)]
    [InlineData(0, CombatGrid.UndergroundPlayableRows - 1, true, true)]
    [InlineData(-1, 0, false, false)]
    public void UndergroundFightsUseFewerRowsThanTheGridHas(
        int column, int row, bool underground, bool playable) {
        // The grid keeps its full height underground; the bound belongs to the FIGHT.
        Assert.Equal(playable, CombatArenaPlacement.IsPlayable(column, row, underground));
    }
}
