namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Character;
using Xunit;

/// <summary>
/// The two ways a modifier gets INTO the table — <c>cspell_try_add_status_effect</c>
/// (CSPELL.C:1053) and item-use effect category 0x12 (ITEMUSE.C:326). Those are the only two
/// callers of <c>stat_modifier_table_insert</c> in the whole game.
/// </summary>
public class StatModifierInsertionTests {
    private const int StrengthMask = 1 << 3;
    private const int OtherMask = 1 << 5;

    private static ActorStatModifiers.Slot Spell(int mask) =>
        new(ActorStatModifiers.SpellStatusFlags, mask, -20, appliedAt: 100, expiresAt: 200);

    private static ActorStatModifiers.Slot Item(int mask) =>
        new(0x0208, mask, -20, appliedAt: 100, expiresAt: 5000);   // Expires | cost 8

    private static ActorStatModifiers.Slot Empty() => new(0, 0, 0, 0, 0);

    [Fact]
    public void TwoCastsOfTheSameDebuffSTACK() {
        // *** The rule that reads backwards. *** A spell status is blocked only by a slot that is
        // NOT itself a spell status, so casting the same debuff twice fills two slots.
        Assert.False(ActorStatModifiers.SpellStatusIsBlocked(
            new[] { Spell(StrengthMask), Empty() }, StrengthMask));
    }

    [Fact]
    public void AnItemsModifierOnThatStatShutsTheSpellOut() {
        Assert.True(ActorStatModifiers.SpellStatusIsBlocked(
            new[] { Item(StrengthMask) }, StrengthMask));
        Assert.False(ActorStatModifiers.SpellStatusIsBlocked(
            new[] { Item(OtherMask) }, StrengthMask), "a different stat is no obstacle");
    }

    [Fact]
    public void TheItemRuleIsStricter_ANYSlotOnThatStatBlocksIt() {
        // No 0x100 exemption on this side: a spell status DOES block a potion, where the reverse
        // is not true. The asymmetry is the point.
        Assert.True(ActorStatModifiers.ItemModifierIsBlocked(
            new[] { Spell(StrengthMask) }, StrengthMask));
        Assert.True(ActorStatModifiers.ItemModifierIsBlocked(
            new[] { Item(StrengthMask) }, StrengthMask));
    }

    [Fact]
    public void AnEmptyTableBlocksNothingEitherWay() {
        var empty = new[] { Empty(), Empty() };
        Assert.False(ActorStatModifiers.SpellStatusIsBlocked(empty, StrengthMask));
        Assert.False(ActorStatModifiers.ItemModifierIsBlocked(empty, StrengthMask));
        Assert.False(ActorStatModifiers.SpellStatusIsBlocked(null, StrengthMask));
        Assert.False(ActorStatModifiers.ItemModifierIsBlocked(null, StrengthMask));
    }

    [Fact]
    public void ASpellStatusAppliesInCOMBATONLY() {
        // 0x100 IS the CombatOnly bit, so the three spell call sites — an accuracy debuff and a
        // strength drain — vanish the moment the fight ends and come back when the next one
        // starts. Reading 0x100 as an opaque "status" marker loses that entirely.
        Assert.Equal((int)ActorStatModifiers.ModifierFlags.CombatOnly,
            ActorStatModifiers.SpellStatusFlags);

        var slot = Spell(StrengthMask);
        Assert.Equal(50, ActorStatModifiers.Apply(slot, 50, inCombat: false, gameTime: 150, out _));
        Assert.Equal(30, ActorStatModifiers.Apply(slot, 50, inCombat: true, gameTime: 150, out _));
    }

    [Fact]
    public void ASpellStatusNeverExpiresThroughThisTable() {
        // No Expires bit, so Apply never consults ExpiresAt. The routine DOES store a value there
        // (game_time << 1), and treating that as a deadline would give a lapse that drifts further
        // away the longer the game has run.
        Assert.Equal(0,
            ActorStatModifiers.SpellStatusFlags & (int)ActorStatModifiers.ModifierFlags.Expires);

        var slot = Spell(StrengthMask);
        int applied = ActorStatModifiers.Apply(slot, 50, inCombat: true, gameTime: 999999,
            out bool expired);
        Assert.False(expired, "long past the written expiry, and still not expired");
        Assert.Equal(30, applied);
    }

    [Fact]
    public void ASpellStatusIsAlwaysTheFirstThingEvicted() {
        // Cost is the LOW byte of the flags word, and a spell status writes 0x100 — cost zero.
        Assert.Equal(0, ActorStatModifiers.CostOf(ActorStatModifiers.SpellStatusFlags));
        Assert.True(ActorStatModifiers.CostOf(0x0208) > 0);
    }

    [Fact]
    public void AnItemsDurationIsCountedIn1800UnitPoints() {
        Assert.Equal(0x708, ActorStatModifiers.ItemDurationUnit);
        Assert.Equal(1000u + (3u * 1800u), ActorStatModifiers.ItemExpiryAt(1000, 3));
        Assert.Equal(1000u, ActorStatModifiers.ItemExpiryAt(1000, 0));
    }
}
