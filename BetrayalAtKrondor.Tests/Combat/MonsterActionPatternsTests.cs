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

    [Fact]
    public void ThePatternTwoRowsDecodeToTheShippedWalks() {
        // Shooters start at attempt 1: 8, 3, 7, 4, 5, 6, 1 (CBTAITRN.C:331 reads the table from index 1).
        var shooter = new List<MonsterActionPatterns.Slot>();
        for (int a = MonsterActionPatterns.CrossbowFirstAttempt; a < MonsterActionPatterns.SlotCount; a++) {
            shooter.Add(MonsterActionPatterns.CrossbowSlot(MonsterActionPatterns.CrossbowSlotFor(2, a)));
        }
        Assert.Equal(MonsterActionPatterns.SlotKind.Engage, shooter[0].Kind);
        Assert.Equal((10, TargetRole.Spellcaster), (shooter[1].Radius, shooter[1].Role));
        Assert.Equal((10, TargetRole.MissileCapable), (shooter[2].Radius, shooter[2].Role));
        Assert.Equal((MonsterActionPatterns.SlotKind.Follow, 6, TargetRole.Anyone),
            (shooter[6].Kind, shooter[6].Radius, shooter[6].Role));

        // Melee walks from attempt 0: follow anyone within 6 first, then role searches, rest last.
        MonsterActionPatterns.Slot first = MonsterActionPatterns.MeleeMoveSlot(MonsterActionPatterns.MeleeMoveSlotFor(2, 0));
        MonsterActionPatterns.Slot last = MonsterActionPatterns.MeleeMoveSlot(MonsterActionPatterns.MeleeMoveSlotFor(2, 7));
        Assert.Equal((MonsterActionPatterns.SlotKind.Follow, 6, TargetRole.Anyone), (first.Kind, first.Radius, first.Role));
        Assert.Equal(MonsterActionPatterns.SlotKind.RestWhenWorn, last.Kind);
        Assert.Equal(MonsterActionPatterns.SlotKind.None, MonsterActionPatterns.CrossbowSlot(0).Kind);
    }

    [Fact]
    public void TheFallbacksRollAgainstTheirOwnThresholds() {
        Assert.True(MonsterActionPatterns.CrossbowFallbackAdvances(74, 50));
        Assert.False(MonsterActionPatterns.CrossbowFallbackAdvances(75, 99));
        Assert.True(MonsterActionPatterns.CrossbowFallbackAdvances(99, 100));
        Assert.True(MonsterActionPatterns.LowHealthRests(74, 3, 79));
        Assert.False(MonsterActionPatterns.LowHealthRests(74, 2, 0));
        Assert.Equal(1, MonsterActionPatterns.CasterAdvanceSteps(4, 2));
        Assert.Equal(3, MonsterActionPatterns.CasterAdvanceSteps(4, 9));
        Assert.Equal(4, MonsterActionPatterns.CasterAdvanceSteps(4, 100));
    }
}
