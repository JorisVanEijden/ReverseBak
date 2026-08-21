namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// Cannons firing — the mechanic this task had recorded as not existing.
/// </summary>
public class CannonLineTests {
    private static List<(int, int, int)> Elements(params (int Type, int X, int Y)[] items) {
        var list = new List<(int, int, int)>();
        foreach ((int t, int x, int y) in items) {
            list.Add((t, x, y));
        }
        return list;
    }

    // Negative ids are markers; -11 lays CannonEast, -10 CannonWest, -12 CannonNorth, -13 CannonSouth.
    private const int LaysCannonEast = -11;
    private const int LaysCannonWest = -10;
    private const int LaysCannonNorth = -12;
    private const int LaysCannonSouth = -13;

    [Fact]
    public void ACannonDownTheLineFires() {
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((LaysCannonEast, 1, 5)));

        IReadOnlyList<CannonLine.Shot> shots = CannonLine.ShotsOn(puzzle, 4, 5);

        Assert.Single(shots);
        Assert.Equal(CombatTerrain.CannonEast, shots[0].Cannon);
        // The shot originates at the cannon, which is where the original stands its stub caster.
        Assert.Equal(1, shots[0].X);
        Assert.Equal(5, shots[0].Y);
    }

    [Fact]
    public void EachDirectionOnlyAnswersToItsOwnCannonKind() {
        // *** The whole trick of the original. *** Scanning west hunts terrain 11 specifically. A
        // cannon sitting in exactly the same place facing another way is not a shot — it is an
        // obstruction, because its element id is not the transparent one, so it stops the scan.
        TrapPuzzle wrongWay = TrapPuzzleBuilder.Build(Elements((LaysCannonWest, 1, 5)));
        Assert.Empty(CannonLine.ShotsOn(wrongWay, 4, 5));

        // ...and that same kind DOES fire from the opposite side.
        TrapPuzzle rightWay = TrapPuzzleBuilder.Build(Elements((LaysCannonWest, 6, 5)));
        Assert.Single(CannonLine.ShotsOn(rightWay, 4, 5));
    }

    [Fact]
    public void AllFourCanFireAtOnce() {
        // step_search runs every branch with preferred_dir < 0, so a crossfire is four shots in one
        // instant rather than the nearest one winning.
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements(
            (LaysCannonEast, 1, 5), (LaysCannonWest, 6, 5),
            (LaysCannonNorth, 4, 2), (LaysCannonSouth, 4, 8)));

        Assert.Equal(4, CannonLine.ShotsOn(puzzle, 4, 5).Count);
    }

    [Fact]
    public void StandingOnACannonIsNotBeingShotByIt() {
        // The scan steps before it looks, so the actor's own tile is never tested. Without that a
        // cannon would fire on whoever was pushed onto it, every step, for ever.
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((LaysCannonEast, 4, 5)));

        Assert.Empty(CannonLine.ShotsOn(puzzle, 4, 5));
    }

    [Fact]
    public void ALivingBodyIsCoverAndACorpseIsNot() {
        // combatgrid_tile_blockd_cmbt answers false for a dead combatant, so stepping behind a
        // fallen ally does not shield you. The caller supplies liveness because the grid's own
        // occupancy also carries elements.
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements((LaysCannonEast, 1, 5)));

        Assert.Empty(CannonLine.ShotsOn(puzzle, 4, 5, (x, y) => x == 2 && y == 5));
        Assert.Single(CannonLine.ShotsOn(puzzle, 4, 5, (x, y) => false));
    }

    [Fact]
    public void ONEHalfOfThePushablePairStopsAShotAndTheOtherDoesNot() {
        // *** Not a tidy rule, and that is the point. *** Ids 9 and 10 are both pushables, but the
        // original's test is `paged_id != 10`, so 9 blocks a shot and 10 is shot straight through.
        // A port that generalised this to "pushables are transparent" would let one of them shield.
        TrapPuzzle blocks = TrapPuzzleBuilder.Build(Elements((LaysCannonEast, 1, 5), (9, 2, 5)));
        Assert.Empty(CannonLine.ShotsOn(blocks, 4, 5));

        TrapPuzzle transparent = TrapPuzzleBuilder.Build(Elements((LaysCannonEast, 1, 5), (10, 2, 5)));
        Assert.Single(CannonLine.ShotsOn(transparent, 4, 5));
    }

    [Fact]
    public void ACrystalStopsAShotButBareCrystalGroundDoesNot() {
        // The element blocks, not the terrain: a crystal standing in the way is an obstruction,
        // while the ground it leaves behind when destroyed shields nobody.
        TrapPuzzle withCrystal = TrapPuzzleBuilder.Build(Elements((LaysCannonEast, 1, 5), (7, 2, 5)));
        Assert.Empty(CannonLine.ShotsOn(withCrystal, 4, 5));

        TrapPuzzle bareGround = TrapPuzzleBuilder.Build(Elements((LaysCannonEast, 1, 5)));
        bareGround.Grid.SetTerrain(2, 5, CombatTerrain.Crystal);
        Assert.Single(CannonLine.ShotsOn(bareGround, 4, 5));
    }

    [Fact]
    public void AnEmptyLineToTheEdgeIsNoShotRatherThanAWalkOffTheGrid() {
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements());

        Assert.Empty(CannonLine.ShotsOn(puzzle, 4, 5));
        Assert.Empty(CannonLine.ShotsOn(puzzle, 0, 0));
        Assert.Empty(CannonLine.ShotsOn(null, 4, 5));
    }

    [Fact]
    public void TheNearestObstructionDecides_NotTheNearestCannon() {
        // Two cannons on one line: the far one is already screened by the near one, which is itself
        // an obstruction rather than a shot when it faces the wrong way.
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(Elements(
            (LaysCannonEast, 0, 5), (LaysCannonWest, 2, 5)));

        Assert.Empty(CannonLine.ShotsOn(puzzle, 4, 5));
    }

    [Fact]
    public void TheShotIsSpellFourAtTwenty() {
        Assert.Equal(4, CannonLine.SpellId);
        Assert.Equal(20, CannonLine.Intensity);
    }
}
