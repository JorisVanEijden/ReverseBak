namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Combat;
using GameData.Resources.Spells;
using System.Linq;
using Xunit;

/// <summary>
/// The encounter's twenty lingering spell-effect slots. The interesting part is the removal defect,
/// which is reproduced on purpose — see <see cref="RemovingTheFirstEffectOrphansEveryLaterOne"/>.
/// </summary>
public class ActiveSpellEffectPoolTests {
    private static Combatant Actor() => new Combatant();

    [Fact]
    public void AFreshPoolIsEntirelyFree() {
        var pool = new ActiveSpellEffectPool();

        Assert.Equal(0, pool.InUse);
        Assert.Equal(0, pool.Allocate());
    }

    [Fact]
    public void ReCastingTheSameSpellStacksRatherThanRefreshing() {
        // Register never consults Find, even though that is exactly the "is this one already
        // affected" question it exists to answer — so both copies age independently and the earlier
        // one expires first. Refreshing instead is the obvious implementation and behaves differently.
        var pool = new ActiveSpellEffectPool();
        Combatant actor = Actor();

        int first = pool.Register(actor, spellNumber: 7, investedCost: 5, duration: 9);
        int second = pool.Register(actor, spellNumber: 7, investedCost: 5, duration: 3);

        Assert.NotEqual(first, second);
        Assert.Equal(2, pool.InUse);
        Assert.Equal(9, pool[first].Duration);
        Assert.Equal(3, pool[second].Duration);
    }

    [Fact]
    public void FindAnswersWithTheFirstOfTheStackedCopies() {
        var pool = new ActiveSpellEffectPool();
        Combatant actor = Actor();

        int first = pool.Register(actor, spellNumber: 7, investedCost: 5, duration: 9);
        pool.Register(actor, spellNumber: 7, investedCost: 5, duration: 3);

        Assert.Equal(first, pool.Find(actor, 7));
    }

    [Fact]
    public void TheFirstEffectBecomesTheActorsChainHead() {
        var pool = new ActiveSpellEffectPool();
        Combatant actor = Actor();

        int slot = pool.Register(actor, spellNumber: 7, investedCost: 12, duration: 4);

        Assert.Equal(slot, actor.ActiveEffectSlot);
        Assert.Equal(7, pool[slot].SpellNumber);
        Assert.Equal(12, pool[slot].InvestedCost);
        Assert.Equal(4, pool[slot].Duration);
        Assert.Equal(0, pool[slot].Age);
        Assert.Equal(ActiveSpellEffectPool.None, pool[slot].Next);
    }

    [Fact]
    public void FurtherEffectsAppendToTheEndOfTheChain() {
        var pool = new ActiveSpellEffectPool();
        Combatant actor = Actor();

        pool.Register(actor, 7, 1, 1);
        pool.Register(actor, 8, 1, 1);
        pool.Register(actor, 9, 1, 1);

        Assert.Equal(new[] { 7, 8, 9 }, pool.EffectsOf(actor).Select(e => e.SpellNumber));
        Assert.Equal(3, pool.InUse);
    }

    [Fact]
    public void TwoActorsKeepSeparateChainsInTheSamePool() {
        var pool = new ActiveSpellEffectPool();
        Combatant a = Actor(), b = Actor();

        pool.Register(a, 7, 1, 1);
        pool.Register(b, 8, 1, 1);
        pool.Register(a, 9, 1, 1);

        Assert.Equal(new[] { 7, 9 }, pool.EffectsOf(a).Select(e => e.SpellNumber));
        Assert.Equal(new[] { 8 }, pool.EffectsOf(b).Select(e => e.SpellNumber));
    }

    [Fact]
    public void RemovingTheFirstEffectOrphansEveryLaterOne() {
        // FAITHFUL TO A DEFECT (0x66a3a): the head is set to -1 rather than to the removed node's
        // successor. The later effects stay marked in use, so they are neither active nor reusable.
        var pool = new ActiveSpellEffectPool();
        Combatant actor = Actor();
        int first = pool.Register(actor, 7, 1, 1);
        pool.Register(actor, 8, 1, 1);
        pool.Register(actor, 9, 1, 1);

        pool.Remove(actor, first);

        Assert.Empty(pool.EffectsOf(actor));                  // all three gone from the actor
        Assert.Equal(ActiveSpellEffectPool.None, actor.ActiveEffectSlot);
        Assert.Equal(2, pool.InUse);                          // but two slots are still consumed
    }

    [Fact]
    public void RemovingAnyOtherEffectUnlinksCorrectly() {
        var pool = new ActiveSpellEffectPool();
        Combatant actor = Actor();
        pool.Register(actor, 7, 1, 1);
        int middle = pool.Register(actor, 8, 1, 1);
        pool.Register(actor, 9, 1, 1);

        pool.Remove(actor, middle);

        Assert.Equal(new[] { 7, 9 }, pool.EffectsOf(actor).Select(e => e.SpellNumber));
        Assert.Equal(2, pool.InUse);   // properly released
    }

    [Fact]
    public void ClearingAnActorIsTheOnlyPathThatReliablyFreesItsSlots() {
        var pool = new ActiveSpellEffectPool();
        Combatant actor = Actor();
        pool.Register(actor, 7, 1, 1);
        pool.Register(actor, 8, 1, 1);
        pool.Register(actor, 9, 1, 1);

        pool.ClearActor(actor);

        Assert.Equal(0, pool.InUse);
        Assert.Equal(ActiveSpellEffectPool.None, actor.ActiveEffectSlot);
    }

    [Fact]
    public void AFullPoolSilentlyRecordsNothing() {
        var pool = new ActiveSpellEffectPool();
        Combatant actor = Actor();
        for (var i = 0; i < ActiveSpellEffectPool.Capacity; i++) {
            Assert.NotEqual(ActiveSpellEffectPool.None, pool.Register(actor, i, 1, 1));
        }

        int overflow = pool.Register(actor, 99, 1, 1);

        Assert.Equal(ActiveSpellEffectPool.None, overflow);
        Assert.Equal(ActiveSpellEffectPool.Capacity, pool.InUse);
        Assert.DoesNotContain(pool.EffectsOf(actor), e => e.SpellNumber == 99);
    }

    [Fact]
    public void TheOrphanLeakCanExhaustThePoolWithinOneEncounter() {
        // Twenty head-removals is all it takes, which is why this is worth knowing about rather
        // than being a curiosity: a long fight stops accepting new effects entirely.
        var pool = new ActiveSpellEffectPool();
        Combatant actor = Actor();

        for (var round = 0; round < ActiveSpellEffectPool.Capacity / 2; round++) {
            int head = pool.Register(actor, 1, 1, 1);
            pool.Register(actor, 2, 1, 1);
            pool.Remove(actor, head);      // drops both from the actor, frees only one
        }

        Assert.Equal(ActiveSpellEffectPool.Capacity / 2, pool.InUse);
        Assert.Empty(pool.EffectsOf(actor));
    }

    [Fact]
    public void AReleasedSlotIsReusedBecauseTheAllocatorOnlyLooksAtTheSpellNumber() {
        var pool = new ActiveSpellEffectPool();
        Combatant actor = Actor();
        int first = pool.Register(actor, 7, 1, 1);
        pool.Remove(actor, first);

        Assert.Equal(first, pool.Register(actor, 8, 1, 1));
    }

    [Fact]
    public void FindLocatesAnActorsEffectBySpell() {
        var pool = new ActiveSpellEffectPool();
        Combatant actor = Actor(), other = Actor();
        pool.Register(actor, 7, 1, 1);
        int wanted = pool.Register(actor, 8, 1, 1);
        pool.Register(other, 8, 1, 1);

        Assert.Equal(wanted, pool.Find(actor, 8));
        Assert.Equal(ActiveSpellEffectPool.None, pool.Find(actor, 42));
    }

    [Fact]
    public void ResetReturnsTheWholePoolIncludingLeakedSlots() {
        var pool = new ActiveSpellEffectPool();
        Combatant actor = Actor();
        int head = pool.Register(actor, 7, 1, 1);
        pool.Register(actor, 8, 1, 1);
        pool.Remove(actor, head);

        pool.Reset();

        Assert.Equal(0, pool.InUse);
    }

    [Fact]
    public void ATickAgesEveryEffectByOneRound() {
        var pool = new ActiveSpellEffectPool();
        Combatant actor = Actor();
        pool.Register(actor, 7, 1, duration: 3);
        pool.Register(actor, 8, 1, duration: 3);

        Assert.False(pool.TickActor(actor));

        Assert.Equal(new[] { 2, 2 }, pool.EffectsOf(actor).Select(e => e.Duration));
    }

    [Fact]
    public void AnEffectIsReleasedWhenItsDurationRunsOut() {
        // The expiring effect must not be the head, or the defect below takes over and the whole
        // chain goes with it — which is a different behaviour, pinned separately.
        var pool = new ActiveSpellEffectPool();
        Combatant actor = Actor();
        pool.Register(actor, 7, 1, duration: 5);
        pool.Register(actor, 8, 1, duration: 1);

        pool.TickActor(actor);

        Assert.Equal(new[] { 7 }, pool.EffectsOf(actor).Select(e => e.SpellNumber));
        Assert.Equal(1, pool.InUse);
    }

    [Fact]
    public void EffectsSurvivingTheRoundTheHeadExpiredAreStrandedForever() {
        // The walk captures each successor before releasing, so the survivors still age THIS round —
        // but the head is now -1, so the next tick starts at nothing and they are never aged again.
        // This is the mechanism behind the pool exhaustion.
        var pool = new ActiveSpellEffectPool();
        Combatant actor = Actor();
        pool.Register(actor, 7, 1, duration: 1);   // expires on the first tick
        pool.Register(actor, 8, 1, duration: 5);   // survives it

        pool.TickActor(actor);

        Assert.Empty(pool.EffectsOf(actor));                  // unreachable from the actor
        Assert.Equal(1, pool.InUse);                          // but still holding a slot

        // Ticking forever will not reclaim it.
        for (var i = 0; i < 50; i++) {
            pool.TickActor(actor);
        }
        Assert.Equal(1, pool.InUse);
    }

    [Fact]
    public void SurvivorsAreStillAgedInTheRoundTheHeadExpires() {
        var pool = new ActiveSpellEffectPool();
        Combatant actor = Actor();
        int head = pool.Register(actor, 7, 1, duration: 1);
        int second = pool.Register(actor, 8, 1, duration: 5);

        pool.TickActor(actor);

        Assert.Equal(4, pool[second].Duration); // aged once despite the head going first
        Assert.Equal(ActiveSpellEffectPool.None, pool[head].SpellNumber);
    }

    [Fact]
    public void AnExpiringSecondEffectIsStillReleasedAfterTheHeadWent() {
        // Both expire on the same tick: the head orphans the chain, but the survivor's own release
        // still frees its slot because the free marker does not depend on the chain.
        var pool = new ActiveSpellEffectPool();
        Combatant actor = Actor();
        pool.Register(actor, 7, 1, duration: 1);
        pool.Register(actor, 8, 1, duration: 1);

        pool.TickActor(actor);

        Assert.Equal(0, pool.InUse);
    }

    [Fact]
    public void OnlyDannonsDelusionsTakesItsActorOffTheField() {
        // It puts an illusory combatant on the grid; expiry fires Final Rest on it and removes it.
        var pool = new ActiveSpellEffectPool();
        Combatant illusion = Actor(), ordinary = Actor();
        pool.Register(illusion, SpellIds.DannonsDelusions, 1, duration: 1);
        pool.Register(ordinary, 8, 1, duration: 1);

        Assert.True(pool.TickActor(illusion));
        Assert.False(pool.TickActor(ordinary));
    }

    [Fact]
    public void AnActorWithNoEffectsTicksToNothing() {
        var pool = new ActiveSpellEffectPool();

        Assert.False(pool.TickActor(Actor()));
    }

    [Fact]
    public void AnOutOfRangeSlotIsIgnoredRatherThanWrittenPastTheEnd() {
        // The original's bound admits index 20 and scribbles one node past the pool.
        var pool = new ActiveSpellEffectPool();
        Combatant actor = Actor();
        pool.Register(actor, 7, 1, 1);

        pool.Remove(actor, ActiveSpellEffectPool.Capacity);

        Assert.Equal(1, pool.InUse);
        Assert.Null(pool[ActiveSpellEffectPool.Capacity]);
    }
}
