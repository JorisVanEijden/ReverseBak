namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Character;
using Xunit;

/// <summary>What the party remembers about a named lock, and how an unfamiliar one reads.</summary>
public class KeyLocksTests {
    [Theory]
    [InlineData(50, 1)]     // Peasant's Key
    [InlineData(60, 8)]
    [InlineData(106, 11)]   // Royal Key of Krondor
    [InlineData(55, 0)]     // no named lock has this difficulty
    public void ADifficultyNamesItsLock(int difficulty, int expected) =>
        Assert.Equal(expected, KeyLocks.NumberFor(difficulty));

    [Fact]
    public void EveryNamedLockMapsToAKeyBetween61And71() {
        for (var n = 1; n <= 11; n++) {
            Assert.Equal(60 + n, PicklockDrop.KeyObjectIdFor(n));
            Assert.Equal(7260 + n, KeyLocks.OpenedGlobal(n));
        }
    }

    [Fact]
    public void AnEasyLockIsWithinReachOfAGoodPicker() =>
        Assert.Equal(KeyLocks.Assessment.WithinSkill, KeyLocks.Assess(50, bestLockPicking: 60));

    [Fact]
    public void TheSameLockIsBeyondAPoorOne() =>
        Assert.Equal(KeyLocks.Assessment.BeyondSkill, KeyLocks.Assess(50, bestLockPicking: 40));

    /// <summary>Past 100 the skill stops being consulted at all, however good it is.</summary>
    [Fact]
    public void AKeyOnlyLockIgnoresSkillEntirely() {
        Assert.Equal(KeyLocks.Assessment.KeyOnly, KeyLocks.Assess(101, bestLockPicking: 100));
        Assert.Equal(KeyLocks.Assessment.KeyOnly, KeyLocks.Assess(106, bestLockPicking: 0));
    }

    [Fact]
    public void PastTheHardestNamedLock_NoKeyHelpsEither() =>
        Assert.Equal(KeyLocks.Assessment.BeyondEveryKey, KeyLocks.Assess(107, bestLockPicking: 100));
}
