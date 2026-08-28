namespace BetrayalAtKrondor.Tests.Combat;

using System.Collections.Generic;
using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The projectile-path trace the bespoke AI routines all gate on
/// (<c>combat_actor_trace_proj_path</c>, CACTOR.C:1161).
/// </summary>
public class CombatLineOfFireTests {
    private static System.Func<int, int, bool> Blocking(params (int X, int Y)[] tiles) {
        var set = new HashSet<(int, int)>(tiles);
        return (x, y) => set.Contains((x, y));
    }

    [Fact]
    public void ACLEARLaneIsClear() =>
        Assert.True(CombatLineOfFire.IsClear(0, 0, 5, 0, Blocking()));

    [Fact]
    public void ABODYOnTheLineBlocks() =>
        Assert.False(CombatLineOfFire.IsClear(0, 0, 5, 0, Blocking((3, 0))));

    [Fact]
    public void THESHOOTERSOwnTileNeverBlocks() =>
        // The ray starts inside it, so a naive walk that tested the origin would make every shooter
        // block itself.
        Assert.True(CombatLineOfFire.IsClear(0, 0, 5, 0, Blocking((0, 0))));

    [Fact]
    public void THETARGETSTileNeverBlocks() =>
        // *** Checked BEFORE the block test on purpose. *** The target is the destination, not an
        // obstacle -- and it is standing on its own tile, so a port that tested occupancy first
        // would find every target unshootable.
        Assert.True(CombatLineOfFire.IsClear(0, 0, 5, 0, Blocking((5, 0))));

    [Fact]
    public void ADJACENTIsAlwaysClear() =>
        Assert.True(CombatLineOfFire.IsClear(3, 3, 3, 4, Blocking((3, 4))));

    [Fact]
    public void THESAMETileIsClear() =>
        Assert.True(CombatLineOfFire.IsClear(2, 2, 2, 2, Blocking((2, 2))));

    [Fact]
    public void ADIAGONALLaneIsWalkedToo() {
        Assert.True(CombatLineOfFire.IsClear(0, 0, 4, 4, Blocking((1, 3), (3, 1))));
        Assert.False(CombatLineOfFire.IsClear(0, 0, 4, 4, Blocking((2, 2))));
    }

    [Fact]
    public void ANULLPredicateMeansNothingBlocks() =>
        Assert.True(CombatLineOfFire.IsClear(0, 0, 5, 5, null));

    // ---- the living/dead rule, which is the one most easily lost --------------------------

    private static Combatant Actor(int x, int y, bool dead = false) => new Combatant {
        X = x, Y = y, Health = 10,
        Flags = dead ? CombatantFlags.Dead : CombatantFlags.Ready,
    };

    private static System.Func<int, int, Combatant> Field(params Combatant[] actors) =>
        (x, y) => System.Array.Find(actors, a => a.X == x && a.Y == y);

    [Fact]
    public void THEDEADDoNotBlock() {
        // *** The original nulls the occupant when CAF_DEAD is set. *** Without this a lane full of
        // corpses is impassable to arrows, and monsters stop firing for no visible reason -- the
        // kind of bug that reads as "the AI is being cautious".
        Combatant shooter = Actor(0, 0);
        Combatant corpse = Actor(3, 0, dead: true);

        Assert.True(CombatLineOfFire.IsClear(0, 0, 5, 0,
            CombatLineOfFire.BlockedByLivingActor(Field(shooter, corpse), shooter)));
    }

    [Fact]
    public void THELIVINGDoBlock() {
        Combatant shooter = Actor(0, 0);
        Combatant inTheWay = Actor(3, 0);

        Assert.False(CombatLineOfFire.IsClear(0, 0, 5, 0,
            CombatLineOfFire.BlockedByLivingActor(Field(shooter, inTheWay), shooter)));
    }

    [Fact]
    public void THESHOOTERIsExcludedByTheStandardPredicate() {
        Combatant shooter = Actor(2, 0);

        Assert.True(CombatLineOfFire.IsClear(2, 0, 6, 0,
            CombatLineOfFire.BlockedByLivingActor(Field(shooter), shooter)));
    }
}
