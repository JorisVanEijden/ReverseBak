namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// Walking a turn's movement across the grid, and what fires underfoot.
/// </summary>
public class CombatWalkTests {
    private static Combatant Actor(int x, int y) =>
        new Combatant { PartySlot = 1, X = x, Y = y, Health = 200, Stamina = 0, Speed = 5 };

    private static CombatGrid GridWith(CombatTerrain terrain, params (int X, int Y)[] tiles) {
        var grid = new CombatGrid();
        foreach ((int x, int y) in tiles) {
            grid.SetTerrain(x, y, terrain);
        }
        return grid;
    }

    [Fact]
    public void EveryCrystalTileCrossedFires_NotJustTheOneStoppedOn() {
        // *** The rule a port gets wrong. *** Checking the destination makes crossing a hazard field
        // free so long as you stop somewhere safe.
        CombatGrid grid = GridWith(CombatTerrain.Crystal, (2, 5), (3, 5), (4, 5));
        Combatant actor = Actor(1, 5);

        CombatWalk.WalkResult result = CombatWalk.Walk(grid, actor, 4, 5, speed: 5);

        Assert.Equal(3, result.Hazards.Count);
        Assert.All(result.Hazards, h => Assert.Equal(CombatWalk.HazardKind.CrystalGround, h.Kind));
        Assert.Equal(CombatWalk.CrystalDamage, result.Hazards[0].Damage);
        Assert.True(result.Arrived);
    }

    [Fact]
    public void CrystalGroundIsNotSpentButATileTrapIs() {
        // Same switch in the original, opposite persistence: the crystal case does not touch the
        // tile, the trap case writes terrain 0 over it.
        CombatGrid crystal = GridWith(CombatTerrain.Crystal, (2, 5));
        Combatant a = Actor(1, 5);
        Assert.Single(CombatWalk.Walk(crystal, a, 2, 5, 5).Hazards);
        a.X = 1;
        a.Y = 5;
        Assert.Single(CombatWalk.Walk(crystal, a, 2, 5, 5).Hazards);

        CombatGrid trap = GridWith(CombatTerrain.Trap, (2, 5));
        Combatant b = Actor(1, 5);
        Assert.Single(CombatWalk.Walk(trap, b, 2, 5, 5).Hazards);
        b.X = 1;
        b.Y = 5;
        Assert.Empty(CombatWalk.Walk(trap, b, 2, 5, 5).Hazards);
    }

    [Fact]
    public void ATrapReadsItsDamageFromTheTile_NotFromAConstant() {
        CombatGrid grid = GridWith(CombatTerrain.Trap, (2, 5));
        Combatant actor = Actor(1, 5);

        CombatWalk.WalkResult result =
            CombatWalk.Walk(grid, actor, 2, 5, 5, tileTrapDamage: (x, y) => 37);

        Assert.Equal(37, Assert.Single(result.Hazards).Damage);
    }

    [Fact]
    public void AProbeFiresNothing_MovesNobodyAndSpendsNoMovement() {
        // The original saves the position up front and restores it, skips the whole hazard block and
        // never writes the speed back — it is the AI asking "could I get there", with a scratch
        // actor. Firing hazards here would hurt actors for the AI merely thinking.
        CombatGrid grid = GridWith(CombatTerrain.Crystal, (2, 5), (3, 5), (4, 5));
        Combatant actor = Actor(1, 5);

        CombatWalk.WalkResult result = CombatWalk.Walk(grid, actor, 4, 5, speed: 5, probe: true);

        Assert.Empty(result.Hazards);
        Assert.Equal(1, actor.X);
        Assert.Equal(5, actor.Y);
        Assert.Equal(5, result.SpeedRemaining);
    }

    [Fact]
    public void MovementCostsTheSTRAIGHTLINEDistance_EvenWhenNoStepSucceeds() {
        // *** Charged up front, from chebyshev(start, dest). *** An actor walled in on every side
        // still pays the full distance to where it was trying to go; "speed - steps taken" would
        // hand back the whole allowance and let it try again.
        var grid = new CombatGrid();
        grid.SetTerrain(2, 4, CombatTerrain.Wall);
        grid.SetTerrain(2, 5, CombatTerrain.Wall);
        grid.SetTerrain(2, 6, CombatTerrain.Wall);
        Combatant actor = Actor(1, 5);

        CombatWalk.WalkResult result = CombatWalk.Walk(grid, actor, 4, 5, speed: 5);

        Assert.Equal(1, actor.X);
        Assert.False(result.Arrived);
        Assert.False(result.PathClear);
        Assert.Equal(2, result.SpeedRemaining);
    }

    [Fact]
    public void DyingUnderfootEndsTheWalkAndForfeitsTheRest() {
        CombatGrid grid = GridWith(CombatTerrain.Crystal, (2, 5), (3, 5), (4, 5));
        Combatant actor = Actor(1, 5);

        CombatWalk.WalkResult result = CombatWalk.Walk(grid, actor, 4, 5, speed: 5,
            onHazard: (a, h) => a.Flags |= CombatantFlags.Dead);

        Assert.Single(result.Hazards);
        Assert.Equal(2, actor.X);
        Assert.False(result.Arrived);

        // And the forfeit is only of the STEPS, not of the charge: one step was taken out of a
        // distance of three, and the budget is still docked all three. Counting steps taken would
        // hand back 4 here.
        Assert.Equal(2, result.SpeedRemaining);
    }

    [Fact]
    public void TheHazardOnTheARRIVALTileStillFires() {
        // The original zeroes the step counter and falls into the switch in the SAME iteration, so
        // arriving is not an escape from what you arrived on.
        CombatGrid grid = GridWith(CombatTerrain.Crystal, (2, 5));
        Combatant actor = Actor(1, 5);

        CombatWalk.WalkResult result = CombatWalk.Walk(grid, actor, 2, 5, speed: 5);

        Assert.True(result.Arrived);
        Assert.Single(result.Hazards);
    }

    [Fact]
    public void WalkingToTheTileYouAreAlreadyOnStillSetsItOff() {
        // Faithful quirk rather than a design choice: the loop always runs its first iteration, and
        // the hazard is read off wherever the actor stands afterwards.
        CombatGrid grid = GridWith(CombatTerrain.Crystal, (2, 5));
        Combatant actor = Actor(2, 5);

        CombatWalk.WalkResult result = CombatWalk.Walk(grid, actor, 2, 5, speed: 5);

        Assert.Single(result.Hazards);
        Assert.Equal(5, result.SpeedRemaining);
    }

    [Fact]
    public void NoSpeedMeansNoWalkAndNoHazard() {
        CombatGrid grid = GridWith(CombatTerrain.Crystal, (2, 5));
        Combatant actor = Actor(1, 5);

        CombatWalk.WalkResult result = CombatWalk.Walk(grid, actor, 2, 5, speed: 0);

        Assert.Empty(result.Hazards);
        Assert.Equal(1, actor.X);
        Assert.Equal(0, result.SpeedRemaining);
    }

    [Fact]
    public void ACannonOnTheLineShootsTheWalker_ButOnlyOnAPuzzle() {
        // step_search runs after every step, so walking into a cannon's line is enough. Ordinary
        // fights pass no puzzle and cannot have cannons at all.
        var elements = new List<(int, int, int)> { (-11, 0, 5) };  // CannonEast at (0,5)
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(elements);
        Combatant actor = Actor(4, 5);

        CombatWalk.WalkResult shot = CombatWalk.Walk(puzzle.Grid, actor, 2, 5, speed: 5, puzzle: puzzle);
        Assert.Contains(shot.Hazards, h => h.Kind == CombatWalk.HazardKind.CannonShot);
        // The shot carries the cannon's tile, not the victim's — that is where the original stands
        // its stub caster.
        CombatWalk.Hazard cannon = shot.Hazards[0];
        Assert.Equal(0, cannon.SourceX);
        Assert.NotEqual(cannon.SourceX, cannon.X);

        Combatant again = Actor(4, 5);
        Assert.Empty(CombatWalk.Walk(puzzle.Grid, again, 2, 5, speed: 5).Hazards);
    }

    [Fact]
    public void ACannonFiresOnEveryStepTakenInItsLine() {
        // Two steps down the line is two shots, for the same reason crystal ground fires per tile.
        var elements = new List<(int, int, int)> { (-11, 0, 5) };
        TrapPuzzle puzzle = TrapPuzzleBuilder.Build(elements);
        Combatant actor = Actor(5, 5);

        CombatWalk.WalkResult result =
            CombatWalk.Walk(puzzle.Grid, actor, 3, 5, speed: 5, puzzle: puzzle);

        Assert.Equal(2, result.Hazards.Count);
    }

    [Fact]
    public void ANullGridOrActorIsAProgrammingErrorRatherThanAQuietNoWalk() {
        Assert.Throws<System.ArgumentNullException>(
            () => CombatWalk.Walk(null, Actor(1, 5), 2, 5, 5));
        Assert.Throws<System.ArgumentNullException>(
            () => CombatWalk.Walk(new CombatGrid(), null, 2, 5, 5));
    }
}
