namespace BetrayalAtKrondor.Tests.Character;

using GameData;
using GameData.Resources.Character;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// The eight timed stat modifiers per character — the hook <see cref="StatEngine.Get"/> has taken
/// since it was written and nothing has ever filled.
/// </summary>
public class ActorStatModifiersTests {
    private const int Cost = 5;

    private static ActorStatModifiers.Slot Slot(
        ActorStatModifiers.ModifierFlags flags, ActorAttribute attribute, short value,
        uint expiresAt = 0, int cost = Cost) =>
        new ActorStatModifiers.Slot((int)flags | cost, 1 << (int)attribute, value, 0, expiresAt);

    [Fact]
    public void TheBlockSizeCrossChecksWithTheStructAndTheReadersStride() {
        // 6 x 8 x 14 = 672, which is gstate.inc's declared length; 14 is what the reader's
        // `modPtr += 7` on a unsigned short* steps. Two derivations, one number.
        Assert.Equal(672, ActorStatModifiers.BlockSize);
        Assert.Equal(14, ActorStatModifiers.SlotSize);
    }

    [Fact]
    public void AnAbsoluteModifierAdds_andAPercentageScales() {
        ActorStatModifiers.Slot flat = Slot(ActorStatModifiers.ModifierFlags.None,
            ActorAttribute.Strength, 7);
        ActorStatModifiers.Slot pct = Slot(ActorStatModifiers.ModifierFlags.Percentage,
            ActorAttribute.Strength, 50);

        Assert.Equal(27, ActorStatModifiers.Apply(flat, 20, inCombat: false, 0, out _));
        Assert.Equal(30, ActorStatModifiers.Apply(pct, 20, inCombat: false, 0, out _));
    }

    [Fact]
    public void APercentageOfMinusOneHundredZeroesTheStat() {
        // value * (delta + 100) / 100, truncating — the engine's arithmetic throughout.
        ActorStatModifiers.Slot pct = Slot(ActorStatModifiers.ModifierFlags.Percentage,
            ActorAttribute.Strength, -100);

        Assert.Equal(0, ActorStatModifiers.Apply(pct, 55, inCombat: false, 0, out _));
    }

    [Fact]
    public void ACombatOnlyModifierIsSkippedOutOfCombat() {
        ActorStatModifiers.Slot slot = Slot(ActorStatModifiers.ModifierFlags.CombatOnly,
            ActorAttribute.Strength, 7);

        Assert.Equal(20, ActorStatModifiers.Apply(slot, 20, inCombat: false, 0, out _));
        Assert.Equal(27, ActorStatModifiers.Apply(slot, 20, inCombat: true, 0, out _));
    }

    [Fact]
    public void ACombatOnlyModifierDoesNotEXPIREOutOfCombatEither() {
        // *** The gate comes first and the expiry sits INSIDE it. *** So a combat buff survives any
        // amount of walking around and comes back at full strength. Testing the expiry first would
        // quietly retire buffs the game keeps.
        ActorStatModifiers.Slot slot = Slot(
            ActorStatModifiers.ModifierFlags.CombatOnly | ActorStatModifiers.ModifierFlags.Expires,
            ActorAttribute.Strength, 7, expiresAt: 100);

        ActorStatModifiers.Apply(slot, 20, inCombat: false, gameTime: 9999, out bool expired);
        Assert.False(expired, "long past its expiry, and still not retired");

        ActorStatModifiers.Apply(slot, 20, inCombat: true, gameTime: 9999, out bool inFight);
        Assert.True(inFight, "in combat it is read, and then it lapses");
    }

    [Fact]
    public void AnExpiredModifierContributesNOTHINGOnTheReadThatRetiresIt() {
        // The expiry check runs BEFORE the value is applied, so it does not get one last hit.
        ActorStatModifiers.Slot slot = Slot(ActorStatModifiers.ModifierFlags.Expires,
            ActorAttribute.Strength, 7, expiresAt: 100);

        int value = ActorStatModifiers.Apply(slot, 20, inCombat: false, gameTime: 101, out bool expired);

        Assert.True(expired);
        Assert.Equal(20, value);
    }

    [Fact]
    public void WithoutTheExpiresBitItIsPermanent() {
        ActorStatModifiers.Slot slot = Slot(ActorStatModifiers.ModifierFlags.None,
            ActorAttribute.Strength, 7, expiresAt: 1);

        Assert.Equal(27, ActorStatModifiers.Apply(slot, 20, inCombat: false, gameTime: 9999, out bool expired));
        Assert.False(expired, "the expiry field is only read when the bit says to");
    }

    [Fact]
    public void ASlotForAnotherAttributeIsNotEvenLookedAt() {
        // The mask is tested before Apply is called, which is also why such a slot never gets its
        // expiry checked — retirement is a side effect of being read for a MATCHING stat.
        ActorStatModifiers.Slot slot = Slot(ActorStatModifiers.ModifierFlags.None,
            ActorAttribute.Strength, 7);

        Assert.True(ActorStatModifiers.Affects(slot, ActorAttribute.Strength));
        Assert.False(ActorStatModifiers.Affects(slot, ActorAttribute.Speed));
    }

    [Fact]
    public void AnEmptySlotWinsOutrightOverACheaperFullOne() {
        var slots = new List<ActorStatModifiers.Slot> {
            Slot(ActorStatModifiers.ModifierFlags.None, ActorAttribute.Strength, 1, cost: 1),
            default,   // empty
            Slot(ActorStatModifiers.ModifierFlags.None, ActorAttribute.Speed, 1, cost: 0),
        };

        Assert.Equal(1, ActorStatModifiers.SlotToFill(slots));
    }

    [Fact]
    public void AFullTableDROPSTheCheapest_itDoesNotRefuse() {
        // *** Stacking buffs on one character is quietly lossy, not an error. ***
        var slots = new List<ActorStatModifiers.Slot>();
        for (var i = 0; i < ActorStatModifiers.SlotsPerCharacter; i++) {
            slots.Add(Slot(ActorStatModifiers.ModifierFlags.None, ActorAttribute.Strength, 1,
                cost: 10 + i));
        }
        slots[5] = Slot(ActorStatModifiers.ModifierFlags.None, ActorAttribute.Strength, 1, cost: 2);

        Assert.Equal(5, ActorStatModifiers.SlotToFill(slots));
    }

    [Fact]
    public void TiesGoToTheEARLIESTSlot() {
        // The scan uses a strict <, so the first of equal costs keeps the claim.
        var slots = new List<ActorStatModifiers.Slot>();
        for (var i = 0; i < ActorStatModifiers.SlotsPerCharacter; i++) {
            slots.Add(Slot(ActorStatModifiers.ModifierFlags.None, ActorAttribute.Strength, 1, cost: 4));
        }

        Assert.Equal(0, ActorStatModifiers.SlotToFill(slots));
    }

    [Fact]
    public void ClearingSelectsOnTheFLAGSWordNotTheStatMask() {
        // *** Both words are masks and they sit next to each other. *** clear_mods_mask ANDs against
        // wMaskFlags, so it selects on the modifier's KIND; reading it as the stat mask clears the
        // wrong modifiers and plausibly.
        // Cost 0 so the low byte cannot muddy the comparison.
        ActorStatModifiers.Slot combat = Slot(ActorStatModifiers.ModifierFlags.CombatOnly,
            ActorAttribute.Strength, 7, cost: 0);

        Assert.True(ActorStatModifiers.ClearedBy(combat, (int)ActorStatModifiers.ModifierFlags.CombatOnly));
        Assert.False(ActorStatModifiers.ClearedBy(combat, (int)ActorStatModifiers.ModifierFlags.Percentage));
        // Its STAT mask is Strength's bit, which is nowhere near the flag bits — clearing by it
        // must not match, and would if the routine were reading the wrong word.
        Assert.False(ActorStatModifiers.ClearedBy(combat, combat.StatMask));
    }

    [Fact]
    public void TheCostIsTheLOWBYTE_notAFlag() {
        Assert.Equal(5, ActorStatModifiers.CostOf((int)ActorStatModifiers.ModifierFlags.CombatOnly | 5));
        Assert.Equal(0xff, ActorStatModifiers.CostOf(0xffff));
    }
}
