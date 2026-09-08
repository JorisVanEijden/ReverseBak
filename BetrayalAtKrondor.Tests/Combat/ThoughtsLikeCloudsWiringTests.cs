namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// Thoughts Like Clouds on the type-2 delivery — TASK-378.
/// </summary>
/// <remarks>
/// The rules were modelled with no production caller at all. These pin the two facts the wiring
/// depends on: which spell the pool entry is, and that the block lands before the charge.
/// </remarks>
public class ThoughtsLikeCloudsWiringTests {
    [Fact]
    public void TheSILENCEIsSpell31_whichIsWhatMakesTheLookupRight() {
        // Type 0x1f in the original's pool is the spell id, not a separate kind — the same rule that
        // makes type 6 the ward. Wiring the lookup to any other id silences nobody.
        Assert.Equal(0x1f, SpellIds.ThoughtsLikeClouds);
    }

    [Fact]
    public void OnlyTheTYPE2DeliveryIsBlocked() {
        // The guard is specific to that routine. Applying it to every cast would make the silence a
        // total shutdown, which is a different and much stronger spell.
        Assert.Equal(SpellCastTail.Delivery.Type2Routine, SpellCastTail.DeliveryFor(2));
        Assert.NotEqual(SpellCastTail.Delivery.Type2Routine, SpellCastTail.DeliveryFor(1));
        Assert.NotEqual(SpellCastTail.Delivery.Type2Routine, SpellCastTail.DeliveryFor(8));
    }

    [Fact]
    public void ASilencedCasterIsBlockedAndAnUnsilencedOneIsNot() {
        Assert.True(SpellCastTail.Type2IsBlocked(casterHasThoughtsLikeClouds: true));
        Assert.False(SpellCastTail.Type2IsBlocked(casterHasThoughtsLikeClouds: false));
    }

    [Fact]
    public void TheBlockIsFREE_soItCannotSitBesideTheDelivery() {
        // Both halves of the rule agree that the test is the routine's first act, ahead of the
        // charge. That is why CombatRuntime checks above ChargeCasterForCast rather than at the
        // delivery branch — placed there, a silenced caster would pay for nothing.
        Assert.True(SpellCastRoutines.HealIsBlockedForFree(casterHasThoughtsLikeClouds: true));
        Assert.False(SpellCastRoutines.HealIsBlockedForFree(casterHasThoughtsLikeClouds: false));
    }
}
