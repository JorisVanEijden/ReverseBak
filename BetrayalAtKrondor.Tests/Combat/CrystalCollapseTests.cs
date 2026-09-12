namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// <c>TrapPuzzle.CollapseUntilIsolated</c> — the crystal-chain collapse
/// (<c>crystalChain_collapseUntilIsolated</c> @0x2F259).
/// </summary>
/// <remarks>
/// <b>A crystal survives by having a neighbour</b>, so collapsing the run it stands on is what takes
/// the chain apart. These pin the two halves that had no expression before TASK-270: the walk erases
/// crystal <i>ground</i>, and isolation is tested at both ends of the run independently.
///
/// <para>Reached in the shipped game only by a Flamecast projectile passing over a standing crystal
/// (<c>Spell_ApplyHitWithProjectile</c> maps spell id 4 -&gt; effect sprite type 2), so there is no
/// walk-into path to test — the entry point is the projectile sweep, not a move.</para>
/// </remarks>
public class CrystalCollapseTests {
    private const int Crystal = CrystalChain.FirstCrystalId;
    private const int Diamond = 9;

    private static TrapPuzzle PuzzleWith(params (int Type, int X, int Y)[] elements) =>
        TrapPuzzleBuilder.Build(new List<(int, int, int)>(elements));

    /// <summary>Paint crystal ground along a row, which is the run the collapse walks.</summary>
    private static void GroundRow(TrapPuzzle puzzle, int fromX, int toX, int y) {
        for (int x = fromX; x <= toX; x++) {
            puzzle.Grid.SetTerrain(x, y, CombatTerrain.Crystal);
        }
    }

    [Fact]
    public void CollapsingErasesTheCrystalGroundAlongTheRun() {
        // The ground erasure is the whole point: terrain normally OUTLIVES the element, and this is
        // the one thing that un-paints it.
        TrapPuzzle puzzle = PuzzleWith((Crystal, 1, 1));
        GroundRow(puzzle, 1, 4, 1);

        puzzle.CollapseUntilIsolated(1, 1);

        for (var x = 1; x <= 4; x++) {
            Assert.NotEqual(CombatTerrain.Crystal, puzzle.Grid.TerrainAt(x, 1));
        }
    }

    /// <summary>
    /// The crystal at the FAR end of the run goes too — the defect the original found.
    /// </summary>
    /// <remarks>
    /// <c>combatgrid_push_back_actor</c> increments inside its own loop condition (CMBTGRID.C:1066),
    /// so the walk stops ON the blocking cell and it is that element the isolation probe kills.
    /// Transcribed the other way round, the walk stopped one cell short and the far end was never
    /// tested: one Black Nimbus wrecked both type-7 crystals in the original and only the aimed one
    /// here (TASK-376, driven in both games 2026-09-12).
    /// </remarks>
    [Fact]
    public void CollapsingARunWrecksTheCrystalAtBOTHENDS() {
        TrapPuzzle puzzle = PuzzleWith((Crystal, 1, 1), (Crystal, 5, 1));
        GroundRow(puzzle, 1, 5, 1);

        puzzle.CollapseUntilIsolated(1, 1);

        Assert.Equal(CrystalChain.WreckElementId, puzzle.Elements[0].ElementId);
        Assert.Equal(CrystalChain.WreckElementId, puzzle.Elements[1].ElementId);
    }

    /// <summary>A run that ends at the grid edge wrecks only the crystal that is on it.</summary>
    /// <remarks>
    /// The original's cursor walks off the grid and its kill is a no-op there; ours would index
    /// outside the grid, so the far-end test is guarded. Nothing to wreck, and no throw.
    /// </remarks>
    [Fact]
    public void ARunThatWalksOffTheGridWrecksOnlyItsOwnEnd() {
        TrapPuzzle puzzle = PuzzleWith((Crystal, 1, 1));
        GroundRow(puzzle, 1, CombatGrid.Width - 1, 1);

        puzzle.CollapseUntilIsolated(1, 1);

        Assert.Equal(CrystalChain.WreckElementId, puzzle.Elements[0].ElementId);
    }

    [Fact]
    public void AnIsolatedTileCollapsesNothing() {
        // One crystal with no run beside it: RunContinues is false from the start, so the loop
        // never runs and the tile is left exactly as it was.
        TrapPuzzle puzzle = PuzzleWith((Crystal, 3, 3));

        Assert.Equal(0, puzzle.CollapseUntilIsolated(3, 3));
        Assert.Equal(CombatTerrain.Crystal, puzzle.Grid.TerrainAt(3, 3));
    }

    [Fact]
    public void TheStandingCrystalIsWreckedOnceItsRunIsGone() {
        TrapPuzzle puzzle = PuzzleWith((Crystal, 1, 1));
        GroundRow(puzzle, 1, 4, 1);

        puzzle.CollapseUntilIsolated(1, 1);

        TrapGridElement element = puzzle.ElementAt(1, 1);
        Assert.NotNull(element);
        Assert.Equal(CrystalChain.WreckElementId, element.ElementId);
        // A wreck is no longer a crystal, so it cannot hold a run together on a later pass.
        Assert.False(CrystalChain.IsCrystalElement(element.ElementId));
    }

    [Fact]
    public void ItTerminates() {
        // The loop guard bounds a bug, not the rule — but a collapse that failed to erase ground
        // would spin forever, and this is the cheapest thing that catches it.
        TrapPuzzle puzzle = PuzzleWith((Crystal, 1, 1));
        GroundRow(puzzle, 1, 7, 1);

        int collapsed = puzzle.CollapseUntilIsolated(1, 1);

        Assert.InRange(collapsed, 1, (CombatGrid.Width * CombatGrid.Height) - 1);
    }

    [Fact]
    public void AnElementOnTheAxisTurnsTheWalkAround() {
        // Ground either side of the origin with a foreign element one step to the RIGHT: the axis
        // is negated so the walk goes LEFT, and the blocker's own tile is never erased.
        TrapPuzzle puzzle = PuzzleWith((Crystal, 3, 1), (Diamond, 4, 1));
        GroundRow(puzzle, 1, 5, 1);

        puzzle.CollapseUntilIsolated(3, 1);

        Assert.NotEqual(CombatTerrain.Crystal, puzzle.Grid.TerrainAt(3, 1));
        Assert.Equal(CombatTerrain.Crystal, puzzle.Grid.TerrainAt(4, 1));
    }

    [Fact]
    public void TheBoxedInRuleIsDeliberatelyNotWired() {
        // NeighboursTakenWhenBoxedIn lives in the axis finder's (0,0) arm, and that arm is
        // unreachable — grid_findRunAxis always answers with a direction. Pinned so nobody "fixes"
        // the absence by wiring a rule the shipped game never runs.
        Assert.Equal(1, CrystalChain.NeighboursTakenWhenBoxedIn);

        TrapPuzzle puzzle = PuzzleWith((Crystal, 1, 1));
        GroundRow(puzzle, 1, 3, 1);
        int before = puzzle.Elements.Count;

        puzzle.CollapseUntilIsolated(1, 1);

        // No extra neighbour is consumed: the element count is unchanged, only an id changed.
        Assert.Equal(before, puzzle.Elements.Count);
    }

    [Fact]
    public void OnlyFlamecastsSpriteIdTakesTheCrystalArm() {
        // The gate is the EFFECT SPRITE id, not "a projectile" — Spell_ApplyHitWithProjectile maps
        // spell 4 to sprite type 2, and a crossbow quarrel flying over the same crystal must do
        // nothing. Pinned as a value comparison because the arm is a caller-side `if` and this is
        // the constant it tests.
        Assert.Equal(2, CombatEffectSprite.Flamecast);
        Assert.NotEqual(CombatEffectSprite.Flamecast, CombatEffectSprite.Shot);
        Assert.NotEqual(CombatEffectSprite.Flamecast, CombatEffectSprite.BaneOfBlackSlayers);
        Assert.NotEqual(CombatEffectSprite.Flamecast, CombatEffectSprite.GenericSpell);
    }

    [Fact]
    public void CollapsingTwiceOnTheSameCellIsNotTheSameAsCollapsingOnce() {
        // Why the sweep remembers its last cell. A flight reports a position every frame, so
        // without that memory one Flamecast would run the collapse on the same crystal repeatedly;
        // this shows the second call is not a no-op and therefore that the guard is load-bearing.
        TrapPuzzle puzzle = PuzzleWith((Crystal, 1, 1));
        GroundRow(puzzle, 1, 5, 1);

        int first = puzzle.CollapseUntilIsolated(1, 1);
        int second = puzzle.CollapseUntilIsolated(1, 1);

        Assert.True(first > 0, "the first pass collapses the run");
        Assert.NotEqual(first, second);
    }
}

