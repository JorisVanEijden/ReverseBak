namespace BetrayalAtKrondor.Tests.World;

using GameData;
using GameData.Resources.Character;
using GameData.Resources.Data;
using GameData.Resources.World;
using System.Collections.Generic;
using System.Linq;
using Xunit;

/// <summary>
/// Falling into a pit (worldcross_dungeon_descent_anim). The headline is that it is not damage —
/// the whole party is put at full Near-death.
/// </summary>
public class PitDescentTests {
    private static ActorConditions Afflicted() {
        var c = new ActorConditions();
        ConditionEngine.Apply(c, ActorCondition.Poisoned, 40);
        ConditionEngine.Apply(c, ActorCondition.Starving, 30);
        return c;
    }

    private static (ActorStat Health, ActorStat Stamina) FullPool() =>
        (new ActorStat { Base = 30, Max = 30 }, new ActorStat { Base = 30, Max = 30 });

    [Fact]
    public void APitOnlyDropsYouUnderground() {
        Assert.True(PitDescent.Triggers(crossingKind: 0xf, zoneKind: 2));
        Assert.False(PitDescent.Triggers(crossingKind: 0xf, zoneKind: 1));
        Assert.False(PitDescent.Triggers(crossingKind: 0xf, zoneKind: 0));
    }

    [Fact]
    public void OtherTerrainDoesNotDropYou() {
        Assert.False(PitDescent.Triggers(crossingKind: 7, zoneKind: 2));
        Assert.False(PitDescent.Triggers(crossingKind: 0, zoneKind: 2));
    }

    [Fact]
    public void TheWholePartyGoesToFullNearDeath() {
        // Not "100 damage" — condition 6 is Near-death, and it lands on everyone regardless of who
        // was leading the party.
        var party = new List<ActorConditions> { new(), new(), new() };

        PitDescent.ApplyToParty(party);

        Assert.All(party, c => Assert.Equal(ActorConditions.MaxRank, c[ActorCondition.NearDeath]));
    }

    [Fact]
    public void TheFallAlsoClearsEveryOtherAffliction() {
        // A consequence of the shared condition rule, not something pit-specific: raising
        // Near-death collapses the actor, which wipes the rest. A poisoned, starving party lands
        // near dead but otherwise clean.
        ActorConditions member = Afflicted();
        Assert.Equal(40, member[ActorCondition.Poisoned]);

        PitDescent.ApplyToParty(new[] { member });

        Assert.Equal(ActorConditions.MaxRank, member[ActorCondition.NearDeath]);
        Assert.Equal(0, member[ActorCondition.Poisoned]);
        Assert.Equal(0, member[ActorCondition.Starving]);
    }

    [Fact]
    public void SupplyingPoolsLetsTheCollapseEmptyThem() {
        var conditions = new List<ActorConditions> { new() };
        (ActorStat Health, ActorStat Stamina) pool = FullPool();

        PitDescent.ApplyToParty(conditions, new[] { pool });

        // Collapse zeroes the pool then refills only to the near-death sliver, so a full party
        // lands with almost nothing left.
        Assert.True(pool.Health.Base + pool.Stamina.Base < 30,
            $"expected a sliver, got {pool.Health.Base + pool.Stamina.Base}");
    }

    [Fact]
    public void ANullMemberIsSkippedRatherThanThrowing() {
        var party = new List<ActorConditions> { new(), null, new() };

        PitDescent.ApplyToParty(party);

        Assert.All(party.Where(c => c != null),
            c => Assert.Equal(ActorConditions.MaxRank, c[ActorCondition.NearDeath]));
    }

    [Fact]
    public void HoldingTheKeyMakesTheFallLonger() {
        Assert.Equal(9, PitDescent.StepsFor(descendKeyHeld: false));
        Assert.Equal(13, PitDescent.StepsFor(descendKeyHeld: true));
    }

    [Fact]
    public void TheCameraDropsEightyUnitsPerFrame() {
        Assert.Equal(0, PitDescent.DropAtStep(0));
        Assert.Equal(-0x50, PitDescent.DropAtStep(1));
        Assert.Equal(-0x50 * 8, PitDescent.DropAtStep(8));
    }

    [Fact]
    public void TheFullFallDropsTheSameDistanceRegardlessOfWhereItStarted() {
        int normal = PitDescent.DropAtStep(PitDescent.StepsFor(false) - 1);
        int held = PitDescent.DropAtStep(PitDescent.StepsFor(true) - 1);

        Assert.Equal(-640, normal);   // 8 * 0x50
        Assert.Equal(-960, held);     // 12 * 0x50
    }
}
