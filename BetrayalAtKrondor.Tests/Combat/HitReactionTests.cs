namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The two-tick recoil — <c>markActorHit</c> @0x6157d and <c>tickHitReactionTimers</c> @0x61598.
/// </summary>
public class HitReactionTests {
    [Fact]
    public void BeginSetsTheKNOCKBACKBitAndNothingElse() {
        (CombatantFlags flags, int timer, int remapIndex) =
            HitReaction.Begin(CombatantFlags.Ready | CombatantFlags.Poisoned, remapIndex: 3);

        Assert.True(flags.HasFlag(CombatantFlags.Knockback));
        // *** THE BIT IS 0x40, NOT 0x64. *** IDA's comments on both functions say 0x64; the
        // encodings say 40 (80 4f 08 40). 0x64 would also set Poisoned and DefendCommand, so this
        // asserts the untouched neighbours rather than only the intended bit.
        Assert.True(flags.HasFlag(CombatantFlags.Ready), "Ready must survive");
        Assert.True(flags.HasFlag(CombatantFlags.Poisoned), "Poisoned must survive");
        Assert.False(flags.HasFlag(CombatantFlags.DefendCommand),
            "0x64 would have set this — it is one of the two bits the wrong constant adds");
        Assert.Equal(HitReaction.Ticks, timer);
        // A palette-remap selector, not a facing: renderCombatGridScene copies it into spriteHitDir
        // and vfx_drawSpriteWithHaloAndHitTint uses it as (value << 8) + 0xA66.
        Assert.Equal(3, remapIndex);
    }

    [Fact]
    public void ItLastsExactlyTwoTicks() {
        (CombatantFlags flags, int timer, _) = HitReaction.Begin(CombatantFlags.None, 0);

        (flags, timer) = HitReaction.Tick(flags, timer);
        Assert.True(flags.HasFlag(CombatantFlags.Knockback), "still recoiling after one tick");

        (flags, timer) = HitReaction.Tick(flags, timer);
        Assert.False(flags.HasFlag(CombatantFlags.Knockback), "cleared on the second");
        Assert.Equal(0, timer);
    }

    [Fact]
    public void AnActorThatWasNeverHitIsNotTicked() {
        // The sweep tests the flag before it decrements, so a timer left over from an earlier
        // recoil is not counted down again.
        (CombatantFlags flags, int timer) = HitReaction.Tick(CombatantFlags.Ready, timer: 7);

        Assert.Equal(CombatantFlags.Ready, flags);
        Assert.Equal(7, timer);
    }

    /// <summary>
    /// The clear tests for EXACTLY zero, which is reproduced rather than corrected.
    /// </summary>
    /// <remarks>
    /// <c>or ax, ax / jnz</c> on the decremented value, not a <c>&lt;= 0</c>. Unreachable in the
    /// shipped game because nothing sets the timer to anything but 2 — pinned so that a port
    /// "tidying" it to <c>&lt;= 0</c> has to argue with this test rather than silently diverge.
    /// </remarks>
    [Fact]
    public void ANegativeTimerCountsDownForeverRatherThanClearing() {
        (CombatantFlags flags, int timer) =
            HitReaction.Tick(CombatantFlags.Knockback, timer: -1);

        Assert.True(flags.HasFlag(CombatantFlags.Knockback));
        Assert.Equal(-2, timer);
    }
}
