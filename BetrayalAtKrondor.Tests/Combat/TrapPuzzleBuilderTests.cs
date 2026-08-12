namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// Laying a TRAPS.DAT encounter onto the combat grid (CMBTGRID.C). The cases below pin the mapping
/// that is easy to get wrong: element ids and terrain kinds are different spaces, and a positive id
/// means something standing on the grid while a negative one is mostly a marker.
/// </summary>
public class TrapPuzzleBuilderTests {
    private static List<(int, int, int)> Elements(params (int Type, int X, int Y)[] items) {
        var list = new List<(int, int, int)>();
        foreach ((int t, int x, int y) in items) {
            list.Add((t, x, y));
        }
        return list;
    }

    [Fact]
    public void ThePuzzleStartsFromTheSameGridAFightWouldUse() {
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements());

        // Load_grid walls the two far corners before anything is placed.
        Assert.True(puzzle.Grid.IsBlocked(0, 0));
        Assert.True(puzzle.Grid.IsBlocked(7, 0));
        Assert.False(puzzle.Grid.IsBlocked(3, 5));
    }

    [Fact]
    public void AnUndergroundPuzzleLosesTheBackRowsJustLikeAnUndergroundFight() {
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements(), underground: true);

        Assert.True(puzzle.Grid.IsBlocked(3, 8));
        Assert.False(puzzle.Grid.IsBlocked(3, 6));
    }

    [Theory]
    [InlineData(7)]
    [InlineData(8)]
    public void ACrystalElementWritesCrystalGroundNotItsOwnId(int elementId) {
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((elementId, 2, 3)));

        Assert.Equal(CombatTerrain.Crystal, puzzle.Grid.TerrainAt(2, 3));
        Assert.Equal(elementId, puzzle.Elements[0].ElementId);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(10)]
    public void ADiamondElementWritesPushableGround(int elementId) {
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((elementId, 4, 5)));

        Assert.Equal(CombatTerrain.Pushable, puzzle.Grid.TerrainAt(4, 5));
        Assert.Equal(elementId, puzzle.Elements[0].ElementId);
    }

    [Fact]
    public void AnElementBlocksTheTileItStandsOn() {
        // Crystal ground on its own is walkable; the crystal standing on it is what blocks.
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((7, 2, 3)));

        Assert.True(puzzle.Grid.IsBlocked(2, 3));
        Assert.True(puzzle.Grid.IsOccupied(2, 3));
    }

    [Theory]
    [InlineData(195)]  // the real one: 117 records in the shipped TRAPS.DAT carry this id
    [InlineData(42)]
    public void AnUnrecognisedPositiveIdIsSkippedEntirelyRatherThanDefaulted(int id) {
        // The original's switch has no default placement: it neither writes terrain nor counts the
        // element, so a record it does not know simply is not there. Id 195 is not a stray — it sits
        // inside the record count in shipping data (encounter 0's first of two records is one), so a
        // port that defaulted unknown ids to "some element" would litter the real puzzles.
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((id, 2, 2)));

        Assert.Empty(puzzle.Elements);
        Assert.Equal(CombatTerrain.Open, puzzle.Grid.TerrainAt(2, 2));
        Assert.False(puzzle.Grid.IsBlocked(2, 2));
    }

    [Theory]
    [InlineData(-10, CombatTerrain.CannonWest)]
    [InlineData(-11, CombatTerrain.CannonEast)]
    [InlineData(-12, CombatTerrain.CannonNorth)]
    [InlineData(-13, CombatTerrain.CannonSouth)]
    public void ACannonRecordsItsFacingAsTerrain(int type, CombatTerrain expected) {
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((type, 1, 2)));

        Assert.Equal(expected, puzzle.Grid.TerrainAt(1, 2));
    }

    [Fact]
    public void ButEveryCannonIsRecordedAsTheSameElementWhicheverWayItFaces() {
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((-10, 1, 1), (-13, 2, 2)));

        Assert.Equal(2, puzzle.Elements.Count);
        Assert.All(puzzle.Elements, e => Assert.Equal(TrapPuzzleBuilder.CannonElementId, e.ElementId));
    }

    [Fact]
    public void PartyMarkersRecordWhereEachSlotStarts() {
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((-15, 1, 1), (-16, 2, 2), (-17, 3, 3)));

        Assert.Equal((1, 1), puzzle.PartyStarts[0]);
        Assert.Equal((2, 2), puzzle.PartyStarts[1]);
        Assert.Equal((3, 3), puzzle.PartyStarts[2]);
    }

    [Fact]
    public void AMarkerForSomebodyWhoIsNotInThePartyPlacesNobody() {
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((-15, 1, 1), (-17, 3, 3)), partySize: 1);

        Assert.NotNull(puzzle.PartyStarts[0]);
        Assert.Null(puzzle.PartyStarts[2]);
        Assert.Equal(CombatTerrain.Open, puzzle.Grid.TerrainAt(3, 3));
    }

    [Fact]
    public void APartyMarkerDoesNotBlockTheTileItMarks() {
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((-15, 1, 1)));

        Assert.Empty(puzzle.Elements);
        Assert.False(puzzle.Grid.IsBlocked(1, 1));
    }

    [Fact]
    public void TheExitCellIsWrittenAsTerrainAndStaysWalkable() {
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((-6, 4, 9)));

        Assert.Equal(CombatTerrain.Exit, puzzle.Grid.TerrainAt(4, 9));
        Assert.False(puzzle.Grid.IsBlocked(4, 9));
        Assert.Empty(puzzle.Elements);
    }

    [Fact]
    public void TheClearFlagMarkerTurnsTheEncounterIntoAPurePuzzle() {
        Assert.True(TrapPuzzleBuilder.Build(Elements((-6, 1, 1))).CombatFlag);
        Assert.False(TrapPuzzleBuilder.Build(Elements((-18, 0, 0))).CombatFlag);
    }

    [Fact]
    public void TheClearFlagMarkerPlacesNothing() {
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((-18, 5, 5)));

        Assert.Empty(puzzle.Elements);
        Assert.Equal(CombatTerrain.Open, puzzle.Grid.TerrainAt(5, 5));
    }

    [Fact]
    public void ElementsKeepTheirFileOrder() {
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((7, 1, 1), (9, 2, 2), (-11, 3, 3)));

        Assert.Equal(new[] { 7, 9, TrapPuzzleBuilder.CannonElementId },
            new[] { puzzle.Elements[0].ElementId, puzzle.Elements[1].ElementId, puzzle.Elements[2].ElementId });
    }

    [Fact]
    public void AnEmptyEncounterLeavesAPlainGrid() {
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(null);

        Assert.Empty(puzzle.Elements);
        Assert.True(puzzle.CombatFlag);
        Assert.All(puzzle.PartyStarts, Assert.Null);
    }
}
