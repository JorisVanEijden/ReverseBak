namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// What keeps a puzzle's crystals alive. The diagonal links and the "crystal ground with something
/// else on it breaks the run" rule are the two a port loses.
/// </summary>
public class CrystalChainTests {
    private const int Crystal = CrystalChain.FirstCrystalId;
    private const int Diamond = 9;

    private static TrapPuzzle PuzzleWith(params (int Type, int X, int Y)[] elements) =>
        TrapPuzzleBuilder.Build(new List<(int, int, int)>(elements));

    [Fact]
    public void ACrystalHeldOnlyByACornerIsStillHeld() {
        // Treating the run as orthogonal would destroy chains the original keeps, and diagonal links
        // are how the shipped puzzles snake across the grid.
        TrapPuzzle puzzle = PuzzleWith((Crystal, 3, 3), (Crystal, 4, 4));

        Assert.True(CrystalChain.RunContinues(puzzle, 3, 3, Crystal));
    }

    [Fact]
    public void ACrystalWithNothingBesideItIsDestroyed() {
        TrapPuzzle puzzle = PuzzleWith((Crystal, 3, 3));

        Assert.True(CrystalChain.IsolationDestroys(puzzle, 3, 3, Crystal));
    }

    [Fact]
    public void AllEightNeighboursCount() {
        for (var dx = -1; dx <= 1; dx++) {
            for (var dy = -1; dy <= 1; dy++) {
                if (dx == 0 && dy == 0) {
                    continue;
                }
                TrapPuzzle puzzle = PuzzleWith((Crystal, 3, 3), (Crystal, 3 + dx, 3 + dy));

                Assert.True(CrystalChain.RunContinues(puzzle, 3, 3, Crystal),
                    $"offset ({dx},{dy}) should carry the run");
            }
        }
    }

    [Fact]
    public void CrystalGroundWithAForeignElementOnItBreaksTheRun() {
        // Both halves of the tile test matter, not just the terrain.
        TrapPuzzle puzzle = PuzzleWith((Crystal, 3, 3), (Crystal, 4, 3));

        Assert.True(CrystalChain.TileCarriesRun(puzzle, 4, 3, Crystal));
        Assert.False(CrystalChain.TileCarriesRun(puzzle, 4, 3, Diamond),
            "crystal ground holding the wrong element does not carry the run");
    }

    [Fact]
    public void OrdinaryGroundNeverCarriesTheRunHoweverEmptyItIs() {
        TrapPuzzle puzzle = PuzzleWith((Crystal, 3, 3));

        Assert.False(CrystalChain.TileCarriesRun(puzzle, 5, 5, Crystal));
    }

    [Fact]
    public void EmptyCrystalGroundCarriesIt() {
        // The ground outlives the element, so a tile whose crystal has gone still links the chain.
        TrapPuzzle puzzle = PuzzleWith((Crystal, 3, 3));
        puzzle.Grid.SetTerrain(4, 3, CombatTerrain.Crystal);

        Assert.Null(puzzle.ElementAt(4, 3));
        Assert.True(CrystalChain.TileCarriesRun(puzzle, 4, 3, Crystal));
        Assert.True(CrystalChain.RunContinues(puzzle, 3, 3, Crystal));
    }

    [Fact]
    public void OffGridNeighboursDoNotProlongAChainAtTheEdge() {
        TrapPuzzle puzzle = PuzzleWith((Crystal, 0, 0));

        Assert.False(CrystalChain.RunContinues(puzzle, 0, 0, Crystal));
    }

    [Fact]
    public void AnyKindTracingRejectsCrystalsAndAcceptsEverythingElse() {
        TrapPuzzle puzzle = PuzzleWith((Crystal, 3, 3));

        Assert.False(CrystalChain.TileCarriesRun(puzzle, 3, 3, -1));
    }

    [Fact]
    public void BothCrystalElementIdsAreCrystals() {
        Assert.True(CrystalChain.IsCrystalElement(7));
        Assert.True(CrystalChain.IsCrystalElement(8));
        Assert.False(CrystalChain.IsCrystalElement(9));
        Assert.False(CrystalChain.IsCrystalElement(CrystalChain.WreckElementId));
    }

    [Fact]
    public void ADeadEndCostsTwoCrystalsNotACascade() {
        // The original stops at the first same-kind neighbour it finds. "Destroy every adjacent
        // crystal" would make dead ends far more powerful than they are.
        Assert.Equal(1, CrystalChain.NeighboursTakenWhenBoxedIn);
    }

    [Fact]
    public void AWreckGetsATerrainOfItsOwnRatherThanBecomingAHole() {
        // Distinct from the pushable kind, though the tile handler answers for the two together in
        // one branch — shared behaviour at one call site, not identity.
        Assert.Equal(14, CrystalChain.WreckTerrain);
        Assert.NotEqual((int)CombatTerrain.Pushable, CrystalChain.WreckTerrain);
        Assert.NotEqual((int)CombatTerrain.Open, CrystalChain.WreckTerrain);
    }

    [Fact]
    public void ANullPuzzleIsNotAnError() {
        Assert.False(CrystalChain.TileCarriesRun(null, 0, 0, Crystal));
        Assert.False(CrystalChain.RunContinues(null, 0, 0, Crystal));
    }
}
