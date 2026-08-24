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
        Assert.True(TrapPuzzleBuilder.Build(Elements((-6, 1, 1))).AllowsRetreat);
        Assert.False(TrapPuzzleBuilder.Build(Elements((-18, 0, 0))).AllowsRetreat);
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
        Assert.True(puzzle.AllowsRetreat);
        Assert.All(puzzle.PartyStarts, Assert.Null);
    }

    // ---- pushing ---------------------------------------------------------------------------

    [Fact]
    public void ADiamondPushedOntoOpenGroundMovesAndTakesItsTerrainWithIt() {
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((9, 3, 3)));

        PushResult result = puzzle.TryPush(3, 3, 0, 1);

        Assert.Equal(PushResult.Moved, result);
        Assert.Equal(CombatTerrain.Open, puzzle.Grid.TerrainAt(3, 3));
        Assert.False(puzzle.Grid.IsBlocked(3, 3));
        Assert.Equal(CombatTerrain.Pushable, puzzle.Grid.TerrainAt(3, 4));
        Assert.True(puzzle.Grid.IsBlocked(3, 4));
        Assert.Equal(3, puzzle.Elements[0].X);
        Assert.Equal(4, puzzle.Elements[0].Y);
    }

    [Fact]
    public void APushIntoAWallIsRefusedAndNothingMoves() {
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((9, 3, 3)));
        puzzle.Grid.SetTerrain(3, 4, CombatTerrain.Wall);

        PushResult result = puzzle.TryPush(3, 3, 0, 1);

        Assert.Equal(PushResult.Blocked, result);
        Assert.Equal(CombatTerrain.Pushable, puzzle.Grid.TerrainAt(3, 3));
        Assert.Equal(3, puzzle.Elements[0].Y);
    }

    [Fact]
    public void APushIntoAnotherElementIsRefused() {
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((9, 3, 3), (9, 3, 4)));

        Assert.Equal(PushResult.Blocked, puzzle.TryPush(3, 3, 0, 1));
    }

    [Fact]
    public void PushingAnEmptyTilePushesNothing() {
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements());

        Assert.Equal(PushResult.NoElement, puzzle.TryPush(3, 3, 0, 1));
    }

    [Fact]
    public void ADiamondShovedOntoCrystalGroundIsDestroyedAndSetsTheCrystalOff() {
        // The puzzle's whole point. The crystal's own element must already be gone — any element
        // blocks — so this is the disarmed-crystal ground left behind.
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((9, 3, 3)));
        puzzle.Grid.SetTerrain(3, 4, CombatTerrain.Crystal);

        PushResult result = puzzle.TryPush(3, 3, 0, 1);

        Assert.Equal(PushResult.CrystalFired, result);
        Assert.False(puzzle.Elements[0].IsOnGrid);
        Assert.Equal(CombatTerrain.Open, puzzle.Grid.TerrainAt(3, 3));
        // The destination keeps its crystal ground; the diamond does not become an obstacle there.
        Assert.Equal(CombatTerrain.Crystal, puzzle.Grid.TerrainAt(3, 4));
        Assert.False(puzzle.Grid.IsOccupied(3, 4));
    }

    [Fact]
    public void ACrystalStillHoldingItsElementCannotBePushedOntoAtAll() {
        // Both are placed by the builder, so the crystal tile is occupied and blocks.
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((9, 3, 3), (7, 3, 4)));

        Assert.Equal(PushResult.Blocked, puzzle.TryPush(3, 3, 0, 1));
    }

    [Fact]
    public void ARemovedElementNoLongerOccupiesAnything() {
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((9, 3, 3)));
        puzzle.Grid.SetTerrain(3, 4, CombatTerrain.Crystal);
        puzzle.TryPush(3, 3, 0, 1);

        Assert.Null(puzzle.ElementAt(3, 4));
        Assert.Null(puzzle.ElementAt(3, 3));
    }

    [Fact]
    public void APushOffTheGridEdgeIsRefused() {
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((9, 3, 12)));

        Assert.Equal(PushResult.Blocked, puzzle.TryPush(3, 12, 0, 1));
    }

    // ---- the crystal line ------------------------------------------------------------------

    [Fact]
    public void AFiringCrystalSweepsTheRunOfCrystalGroundItSitsOn() {
        // A horizontal run of crystal ground with a crystal standing at one end.
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((7, 2, 5)));
        puzzle.Grid.SetTerrain(3, 5, CombatTerrain.Crystal);
        puzzle.Grid.SetTerrain(4, 5, CombatTerrain.Crystal);

        IReadOnlyList<(int X, int Y)> run = puzzle.TraceCrystalLine(2, 5);

        Assert.NotEmpty(run);
        Assert.All(run, t => Assert.Equal(CombatTerrain.Crystal, puzzle.Grid.TerrainAt(t.X, t.Y)));
    }

    [Fact]
    public void ATileWithNoRunSweepsNothing() {
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((7, 2, 5)));

        Assert.Empty(puzzle.TraceCrystalLine(2, 5));
    }

    [Fact]
    public void TheSweepStopsWhereTheCrystalGroundStops() {
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((7, 2, 5)));
        puzzle.Grid.SetTerrain(3, 5, CombatTerrain.Crystal);
        puzzle.Grid.SetTerrain(4, 5, CombatTerrain.Crystal);
        // 5,5 deliberately left open — the run must not reach it.

        IReadOnlyList<(int X, int Y)> run = puzzle.TraceCrystalLine(2, 5);

        Assert.DoesNotContain((5, 5), run);
    }

    [Fact]
    public void TheSweepIsContiguous() {
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((7, 2, 5)));
        puzzle.Grid.SetTerrain(3, 5, CombatTerrain.Crystal);
        puzzle.Grid.SetTerrain(4, 5, CombatTerrain.Crystal);

        IReadOnlyList<(int X, int Y)> run = puzzle.TraceCrystalLine(2, 5);

        for (var i = 1; i < run.Count; i++) {
            int stepX = System.Math.Abs(run[i].X - run[i - 1].X);
            int stepY = System.Math.Abs(run[i].Y - run[i - 1].Y);
            Assert.True(stepX <= 1 && stepY <= 1 && (stepX + stepY) > 0, $"gap between {i - 1} and {i}");
        }
    }

    [Fact]
    public void PushingADiamondIntoACrystalDealsNoDamageItselfItOnlySweeps() {
        // Pinned because it is easy to assume otherwise: the push's only other effect is a sound and
        // a particle burst. The 100 damage belongs to a party member WALKING onto crystal ground.
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((9, 3, 3)));
        puzzle.Grid.SetTerrain(3, 4, CombatTerrain.Crystal);

        PushResult result = puzzle.TryPush(3, 3, 0, 1);

        Assert.Equal(PushResult.CrystalFired, result);
        // Nothing on the result says "damage"; the caller animates the run and plays the burst.
        Assert.Equal(CombatTerrain.Crystal, puzzle.Grid.TerrainAt(3, 4));
    }
}
