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
