namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Character;
using System;
using Xunit;

/// <summary>
/// picklock_screen_handle_drop. Two mechanics behind one screen: picks are a deterministic skill
/// comparison, keys are exact-match with a breakage risk.
/// </summary>
public class PicklockAttemptTests {
    /// <summary>Deterministic stand-in for RND(n): replays a fixed script.</summary>
    private static Func<int, int> Rolls(params int[] results) {
        var i = 0;
        return _ => results[i++];
    }

    private static Func<int, int> Always(int value) => _ => value;

    // ---- lockpicks ------------------------------------------------------------------------

    [Fact]
    public void PicksOpenALockBelowYourSkillWithNoRollAtAll() {
        // Deterministic: the same character either can or cannot open a given lock, every time.
        PicklockAttempt.AttemptResult result =
            PicklockAttempt.WithLockpicks(lockScore: 40, skill: 41, rnd: null, out int awarded);

        Assert.Equal(PicklockAttempt.AttemptResult.Opened, result);
        Assert.Equal(2, awarded);
    }

    [Fact]
    public void TheSkillComparisonIsStrict() {
        Assert.Equal(PicklockAttempt.AttemptResult.Opened,
            PicklockAttempt.WithLockpicks(40, 41, null, out _));
        Assert.NotEqual(PicklockAttempt.AttemptResult.Opened,
            PicklockAttempt.WithLockpicks(40, 40, Always(99), out _));
    }

    [Fact]
    public void ALockAboveOneHundredCanNeverBePicked() {
        // However skilled the picker — it needs its exact key.
        Assert.NotEqual(PicklockAttempt.AttemptResult.Opened,
            PicklockAttempt.WithLockpicks(lockScore: 101, skill: 255, rnd: Always(99), out _));
    }

    [Fact]
    public void AFailedPickSometimesStillTeaches() {
        PicklockAttempt.WithLockpicks(90, 10, Rolls(40, 99), out int learned);
        Assert.Equal(1, learned);

        PicklockAttempt.WithLockpicks(90, 10, Rolls(41, 99), out int nothing);
        Assert.Equal(0, nothing);
    }

    [Fact]
    public void APickSnapsMoreReadilyTheFurtherTheLockIsBeyondYou() {
        // (score - skill) * 2/3: at 90 vs 10 that is 53, so a roll of 53 breaks and 54 does not.
        Assert.Equal(PicklockAttempt.AttemptResult.ToolBroke,
            PicklockAttempt.WithLockpicks(90, 10, Rolls(99, 53), out _));
        Assert.Equal(PicklockAttempt.AttemptResult.Failed,
            PicklockAttempt.WithLockpicks(90, 10, Rolls(99, 54), out _));
    }

    [Fact]
    public void APickCannotBreakOnALockAtOrBelowYourSkill() {
        // The threshold goes negative, and RND is never below zero — but such a lock opens anyway
        // unless it is over 100, so this is the >100 case.
        Assert.Equal(PicklockAttempt.AttemptResult.Failed,
            PicklockAttempt.WithLockpicks(lockScore: 120, skill: 200, rnd: Rolls(99, 0), out _));
    }

    // ---- keys -----------------------------------------------------------------------------

    [Fact]
    public void AKeyMustMatchExactly() {
        Assert.Equal(PicklockAttempt.AttemptResult.Opened,
            PicklockAttempt.WithKey(keyValue: 60, lockScore: 60, skill: 0, rnd: null));
        Assert.NotEqual(PicklockAttempt.AttemptResult.Opened,
            PicklockAttempt.WithKey(61, 60, 0, Always(99)));
    }

    [Fact]
    public void AMoreValuableKeyIsNotABetterKeyOnlyADifferentOne() {
        // No "close enough" and no ordering: 99 does not open a lock of 60.
        Assert.NotEqual(PicklockAttempt.AttemptResult.Opened,
            PicklockAttempt.WithKey(99, 60, 255, Always(99)));
    }

    [Fact]
    public void AMoreValuableKeyIsSaferToTry() {
        Assert.True(PicklockAttempt.KeyBreakThreshold(keyValue: 80, skill: 0)
            < PicklockAttempt.KeyBreakThreshold(keyValue: 20, skill: 0));
    }

    [Fact]
    public void ASkilledPickerBreaksFewerKeys() {
        Assert.True(PicklockAttempt.KeyBreakThreshold(20, skill: 90)
            < PicklockAttempt.KeyBreakThreshold(20, skill: 0));
    }

    [Fact]
    public void TheLocksOwnDifficultyDoesNotAffectBreakage() {
        // The threshold reads the key and the picker only — the lock is not in the formula.
        Assert.Equal(PicklockAttempt.KeyBreakThreshold(20, 30),
            PicklockAttempt.KeyBreakThreshold(20, 30));
        Assert.Equal(PicklockAttempt.AttemptResult.ToolBroke,
            PicklockAttempt.WithKey(20, lockScore: 5, skill: 30, rnd: Always(0)));
        Assert.Equal(PicklockAttempt.AttemptResult.ToolBroke,
            PicklockAttempt.WithKey(20, lockScore: 200, skill: 30, rnd: Always(0)));
    }

    [Fact]
    public void OpeningWithAKeyIsRecordedPerKeyKind() {
        Assert.Equal(7260, PicklockAttempt.PickedWithFlag(0));
        Assert.Equal(7265, PicklockAttempt.PickedWithFlag(5));
    }
}
