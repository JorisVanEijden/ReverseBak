namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Combat;
using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// Which effects stop an actor acting — <c>CanActInCombat</c> @0x63fa2. Only three spells do, and
/// the verdict is now derived from the pool rather than set by hand.
/// </summary>
public class ActiveSpellEffectPoolIncapacitationTests {
    [Fact]
    public void ExactlyThreeSpellsIncapacitate() {
        Assert.Equal(3, ActiveSpellEffectPool.IncapacitatingSpells.Length);
        Assert.Contains(SpellIds.DannonsDelusions, ActiveSpellEffectPool.IncapacitatingSpells);
        Assert.Contains(SpellIds.DespairThyEyes, ActiveSpellEffectPool.IncapacitatingSpells);
        Assert.Contains(SpellIds.GriefOfAThousandNights,
            ActiveSpellEffectPool.IncapacitatingSpells);
    }

    [Fact]
    public void AndTheObviousCandidatesDoNot() {
        // Nothing else in the catalogue counts — not the long-duration buffs, not Mad God's Rage.
        var pool = new ActiveSpellEffectPool();
        var actor = new Combatant();
        pool.Register(actor, SpellIds.MadGodsRage, investedCost: 20, duration: 9);
        pool.Register(actor, SpellIds.SkinOfTheDragon, investedCost: 20, duration: 9);

        Assert.False(pool.IsIncapacitated(actor));
        Assert.False(actor.Incapacitated);
    }

    [Fact]
    public void RegisteringAnIncapacitatingEffectSetsTheVerdict() {
        var pool = new ActiveSpellEffectPool();
        var actor = new Combatant();
        pool.Register(actor, SpellIds.GriefOfAThousandNights, investedCost: 4, duration: 2);

        Assert.True(actor.Incapacitated);
        Assert.False(actor.CanAct(strict: false));
    }

    [Fact]
    public void ItSurvivesAnUnrelatedEffectExpiring() {
        var pool = new ActiveSpellEffectPool();
        var actor = new Combatant();
        pool.Register(actor, SpellIds.DespairThyEyes, investedCost: 2, duration: 5);
        int shortLived = pool.Register(actor, SpellIds.Steelfire, investedCost: 10, duration: 1);

        Assert.NotEqual(ActiveSpellEffectPool.None, shortLived);
        pool.TickActor(actor);

        Assert.True(actor.Incapacitated);
    }

    [Fact]
    public void AndLiftsWhenTheEffectItselfExpires() {
        var pool = new ActiveSpellEffectPool();
        var actor = new Combatant();
        pool.Register(actor, SpellIds.DespairThyEyes, investedCost: 2, duration: 1);
        Assert.True(actor.Incapacitated);

        pool.TickActor(actor);

        Assert.False(actor.Incapacitated);
        Assert.True(actor.CanAct(strict: false));
    }

    [Fact]
    public void ClearingAnActorLiftsItToo() {
        var pool = new ActiveSpellEffectPool();
        var actor = new Combatant();
        pool.Register(actor, SpellIds.DannonsDelusions, investedCost: 5, duration: 9);

        pool.ClearActor(actor);

        Assert.False(actor.Incapacitated);
    }

    [Fact]
    public void TheHeadRemovalDefectStrandsTheVerdictToo() {
        // The head expires, so the chain head goes to -1 and the surviving incapacitating effect is
        // orphaned — unreachable, so the actor is treated as able to act while a slot it can never
        // reclaim still holds Grief of 1000 Nights.
        var pool = new ActiveSpellEffectPool();
        var actor = new Combatant();
        pool.Register(actor, SpellIds.Steelfire, investedCost: 10, duration: 1);
        pool.Register(actor, SpellIds.GriefOfAThousandNights, investedCost: 4, duration: 9);
        Assert.True(actor.Incapacitated);

        pool.TickActor(actor);

        Assert.False(actor.Incapacitated);
        Assert.Equal(1, pool.InUse);
    }

    [Fact]
    public void AnActorWithNoEffectsIsNotIncapacitated() {
        var pool = new ActiveSpellEffectPool();
        Assert.False(pool.IsIncapacitated(new Combatant()));
        Assert.False(pool.IsIncapacitated(null));
    }
}
