namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// The two effect arms whose sound is pure sequence — TASK-117 / TASK-144.
/// </summary>
public class SpellEffectArmSoundTests {
    [Fact]
    public void ONLYTheTwoSequencedArmsAreClaimed() {
        // The other sixteen arms draw things; claiming them here would promise audio that has no
        // rule behind it. Kind 3 is the projectile, already handled elsewhere.
        Assert.True(SpellEffectArmSound.HasSequence(SpellEffectArmSound.StormFlashKind));
        Assert.True(SpellEffectArmSound.HasSequence(SpellEffectArmSound.ParticleBlastKind));
        Assert.False(SpellEffectArmSound.HasSequence(3));
        Assert.False(SpellEffectArmSound.HasSequence(-1));
    }

    [Fact]
    public void TheStormCracksFOURTimes() {
        // A do/while in the original, so four is the count and not "up to four".
        Assert.Equal(4, SpellEffectArmSound.StormFlashCount);
    }

    [Fact]
    public void TheGapIsREROLLEDPerFlash_andZeroIsLegal() {
        // RND(0x28) each time round, so the storm is irregular rather than metronomic — and a zero
        // roll is a real outcome, two cracks landing together.
        Assert.Equal(0x28, SpellEffectArmSound.FlashGapTickBound);
        Assert.Equal(0, SpellEffectArmSound.FlashGapTicks(_ => 0));
        Assert.Equal(39, SpellEffectArmSound.FlashGapTicks(n => n - 1));
        Assert.Equal(0, SpellEffectArmSound.FlashGapTicks(null));
    }

    [Fact]
    public void TheHELDCueIsNotTheCrackingOne() {
        // 17 is started once and held across the sequence; 21 fires per flash. Swapping them would
        // give one long crack and four bursts of static.
        Assert.Equal(0x11, SpellEffectArmSound.StaticCue);
        Assert.Equal(0x15, SpellEffectArmSound.ThunderCue);
        Assert.NotEqual(SpellEffectArmSound.StaticCue, SpellEffectArmSound.ThunderCue);
    }
}
