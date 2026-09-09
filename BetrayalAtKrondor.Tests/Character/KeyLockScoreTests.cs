namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Character;
using Xunit;

/// <summary>
/// A key's kind is an index into the lock-score table, not a lock score.
/// </summary>
/// <remarks>
/// <b>The rule compared the index itself until 2026-09-09</b>, so a key could open only a lock whose
/// score happened to equal its own kind — and the interesting locks, which is all of them above 11,
/// were unopenable by any key in the game.
/// </remarks>
public class KeyLockScoreTests {
    /// <summary>Object ids 61..71 are the eleven keys; kind is <c>objectId - 60</c>.</summary>
    private const int KeyObjectIdBase = 60;

    [Fact]
    public void TheTableIsTheShippedOne() {
        // g_abInvQuizAnswerTable, PICKLOCK.C:29. Thirteen entries, only 1..11 are keys.
        Assert.Equal(
            new[] { 0, 0x32, 0x5a, 0x65, 0x66, 0x67, 0x68, 0x46, 0x3c, 0x50, 0x69, 0x6a, 0 },
            PicklockAttempt.KeyLockScores);
    }

    [Fact]
    public void AKeyKindIsNothingLikeTheScoreItOpens() {
        // The point of the table: if kind and score agreed, the table would be redundant.
        Assert.Equal(101, PicklockAttempt.LockScoreForKeyKind(3));
        Assert.Equal(60, PicklockAttempt.LockScoreForKeyKind(8));
        Assert.Equal(106, PicklockAttempt.LockScoreForKeyKind(11));
    }

    [Fact]
    public void TheChapterOneLadderIsOpenedByKeyObject71() {
        // Zone 11 (720800, 719600) carries LockData.Difficulty 106 and dialog 2300004 — the ladder
        // into the palace that ends chapter 1. Two of object 71 ship in the game's containers.
        const int ladderLock = 106;
        int kind = 71 - KeyObjectIdBase;
        Assert.Equal(PicklockAttempt.AttemptResult.Opened,
            PicklockAttempt.WithKey(kind, ladderLock, skill: 26, rnd: _ => 100));
    }

    [Fact]
    public void UnderTheOldRuleTheLadderWantedAKeyThatDoesNotExist() {
        // objectId - 60 == 106 means objectId 166, and nothing in the shipped containers is above
        // 71. Every other key is refused, which is the correct half of the old behaviour.
        for (var kind = 1; kind <= 11; kind++) {
            if (kind == 11) {
                continue;
            }
            Assert.NotEqual(PicklockAttempt.AttemptResult.Opened,
                PicklockAttempt.WithKey(kind, 106, skill: 26, rnd: _ => 100));
        }
    }

    [Fact]
    public void BreakOddsComeFromTheScoreToo_SoAHighScoringKeyIsSafer() {
        // PICKLOCK.C:130 reads the same table entry it compares with. Kind 11 scores 106, so its
        // threshold is negative and it cannot snap; kind 1 scores 50 and can.
        Assert.True(PicklockAttempt.KeyBreakThreshold(11, skill: 26) < 0);
        Assert.True(PicklockAttempt.KeyBreakThreshold(1, skill: 26) > 0);
        Assert.True(PicklockAttempt.KeyBreakThreshold(1, skill: 90)
            < PicklockAttempt.KeyBreakThreshold(1, skill: 0));
    }
}
