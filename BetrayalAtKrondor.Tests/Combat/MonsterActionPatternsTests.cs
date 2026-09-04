namespace BetrayalAtKrondor.Tests.Combat;

using System.Collections.Generic;
using System.Linq;
using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The crossbow and melee/move action-priority tables — <c>crossbowPattern_actionPriority</c>
/// @0x3B428 and <c>meleeMovePattern_actionPriority</c> @0x3B2E8.
/// </summary>
public class MonsterActionPatternsTests {
    private static IEnumerable<int> Patterns =>
        Enumerable.Range(1, MonsterActionPatterns.MaxPattern);

    private static int[] CrossbowRow(int pattern) =>
        Enumerable.Range(0, MonsterActionPatterns.SlotCount)
            .Select(a => MonsterActionPatterns.CrossbowSlotFor(pattern, a)).ToArray();

    private static int[] MeleeRow(int pattern) =>
        Enumerable.Range(0, MonsterActionPatterns.SlotCount)
            .Select(a => MonsterActionPatterns.MeleeMoveSlotFor(pattern, a)).ToArray();

    [Fact]
    public void EveryRowIsAPermutationOfTheEightSlots() {
        // A transcription slip from the IDA bytes almost always breaks this — it duplicates one
        // slot and drops another — which is what makes it worth asserting over the raw values.
        int[] all = Enumerable.Range(1, MonsterActionPatterns.SlotCount).ToArray();
        foreach (int pattern in Patterns) {
            Assert.Equal(all, CrossbowRow(pattern).OrderBy(s => s).ToArray());
            Assert.Equal(all, MeleeRow(pattern).OrderBy(s => s).ToArray());
        }
    }

    [Fact]
    public void EveryRowBeginsWithItsOwnPatternNumber() {
        // The property that makes "pattern" mean "the action I try first" — and the reason the
        // crossbow turn's start-at-1 is a deliberate skip rather than an off-by-one.
        foreach (int pattern in Patterns) {
            Assert.Equal(pattern, CrossbowRow(pattern)[0]);
            Assert.Equal(pattern, MeleeRow(pattern)[0]);
        }
    }

    [Fact]
    public void PatternZeroActsInNeitherFamily() {
        Assert.False(MonsterActionPatterns.Shoots(0));
        Assert.False(MonsterActionPatterns.Fights(0));
        Assert.Equal(0, MonsterActionPatterns.CrossbowSlotFor(0, 0));
        Assert.Equal(0, MonsterActionPatterns.MeleeMoveSlotFor(0, 0));
    }

    [Fact]
    public void PatternsPastTheTableAreRefusedRatherThanIndexed() {
        // The tables are 1-based with the base one row BEFORE row 1, so an unchecked index does not
        // throw — it reads the neighbouring array and returns plausible-looking rubbish.
        int past = MonsterActionPatterns.MaxPattern + 1;
        Assert.False(MonsterActionPatterns.Shoots(past));
        Assert.False(MonsterActionPatterns.Fights(past));
        Assert.Equal(0, MonsterActionPatterns.CrossbowSlotFor(past, 0));
        Assert.Equal(0, MonsterActionPatterns.MeleeMoveSlotFor(past, 0));
    }

    [Fact]
    public void AttemptsOutsideTheRowAreRefused() {
        Assert.Equal(0, MonsterActionPatterns.CrossbowSlotFor(1, -1));
        Assert.Equal(0, MonsterActionPatterns.CrossbowSlotFor(1, MonsterActionPatterns.SlotCount));
        Assert.Equal(0, MonsterActionPatterns.MeleeMoveSlotFor(1, -1));
        Assert.Equal(0, MonsterActionPatterns.MeleeMoveSlotFor(1, MonsterActionPatterns.SlotCount));
    }

    [Fact]
    public void TheTwoFamiliesStartFromDifferentAttempts() {
        // monster_chooseMeleeMoveAction opens `xor di, di`; monster_chooseCrossbowAction opens
        // `mov di, 1`. Equalising these would change the action order of every shooter in the game,
        // so the difference is pinned rather than left to a comment.
        Assert.Equal(0, MonsterActionPatterns.MeleeMoveFirstAttempt);
        Assert.Equal(1, MonsterActionPatterns.CrossbowFirstAttempt);
    }

    [Fact]
    public void TheCrossbowTurnSkipsItsRowsSelfReferentialFirstEntry() {
        // Consequence of the two constants above: because every row opens with its own pattern
        // number and the crossbow turn starts at attempt 1, a shooter never tries the slot its
        // pattern names — it starts at the fallback order.
        foreach (int pattern in Patterns) {
            Assert.NotEqual(pattern,
                MonsterActionPatterns.CrossbowSlotFor(pattern,
                    MonsterActionPatterns.CrossbowFirstAttempt));
        }
    }
}
