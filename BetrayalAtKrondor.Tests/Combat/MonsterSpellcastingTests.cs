namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// The caster AI: a pattern row that orders eight action slots, and a filter chain that picks the
/// spell at the moment it is needed.
/// </summary>
public class MonsterSpellcastingTests {
    [Fact]
    public void PatternZeroNeverCasts() {
        Assert.False(MonsterSpellcasting.Casts(0));
        Assert.Equal(0, MonsterSpellcasting.SlotFor(0, 0));
    }

    [Fact]
    public void AndNeitherDoesAPatternPastTheTable() {
        Assert.False(MonsterSpellcasting.Casts(9));
        Assert.True(MonsterSpellcasting.Casts(1));
        Assert.True(MonsterSpellcasting.Casts(8));
    }

    [Fact]
    public void EveryRowLeadsWithItsOwnPatternNumber() {
        // Which is what makes the field readable as "the action I try first".
        for (int pattern = 1; pattern <= MonsterSpellcasting.MaxPattern; pattern++) {
            Assert.Equal(pattern, MonsterSpellcasting.SlotFor(pattern, 0));
        }
    }

    [Fact]
    public void AndEveryRowIsAPermutationOfAllEightSlots() {
        for (int pattern = 1; pattern <= MonsterSpellcasting.MaxPattern; pattern++) {
            var seen = new bool[MonsterSpellcasting.SlotCount + 1];
            for (int attempt = 0; attempt < MonsterSpellcasting.SlotCount; attempt++) {
                int slot = MonsterSpellcasting.SlotFor(pattern, attempt);
                Assert.InRange(slot, 1, MonsterSpellcasting.SlotCount);
                Assert.False(seen[slot]);
                seen[slot] = true;
            }
        }
    }

    [Fact]
    public void AnAttemptPastTheRowIsNotASlot() {
        Assert.Equal(0, MonsterSpellcasting.SlotFor(3, 8));
        Assert.Equal(0, MonsterSpellcasting.SlotFor(3, -1));
    }

    [Fact]
    public void ASpentMonsterMakesNoAttemptAtAll() {
        Assert.False(MonsterSpellcasting.WellEnoughToAct(4));
        Assert.True(MonsterSpellcasting.WellEnoughToAct(5));
    }

    [Fact]
    public void NineAttemptsInAHundredAreSkipped() {
        // And a skip advances to the next slot rather than ending the turn.
        Assert.True(MonsterSpellcasting.CommitsToAttempt(90));
        Assert.False(MonsterSpellcasting.CommitsToAttempt(91));
        Assert.False(MonsterSpellcasting.CommitsToAttempt(99));
    }

    [Fact]
    public void TheSlotToTargetModeMapIsNotInOrder() {
        // Slots 2-4 run 0-2 and then it jumps: 5 -> 4, 6 -> 5, 7 -> 3.
        Assert.Equal(0, MonsterSpellcasting.TargetModeOf(2));
        Assert.Equal(1, MonsterSpellcasting.TargetModeOf(3));
        Assert.Equal(2, MonsterSpellcasting.TargetModeOf(4));
        Assert.Equal(4, MonsterSpellcasting.TargetModeOf(5));
        Assert.Equal(5, MonsterSpellcasting.TargetModeOf(6));
        Assert.Equal(3, MonsterSpellcasting.TargetModeOf(7));
    }

    [Fact]
    public void AndAllSixModesAreReachable() {
        var reached = new bool[6];
        for (int slot = 2; slot <= 7; slot++) {
            reached[MonsterSpellcasting.TargetModeOf(slot)] = true;
        }
        Assert.All(reached, Assert.True);
    }

    [Fact]
    public void TheTwoOuterSlotsAreNotTargetedCasts() {
        Assert.Equal(MonsterSpellcasting.SlotAction.SpecialFirst, MonsterSpellcasting.ActionOf(1));
        Assert.Equal(MonsterSpellcasting.SlotAction.SpecialLast, MonsterSpellcasting.ActionOf(8));
        Assert.Equal(-1, MonsterSpellcasting.TargetModeOf(1));
        Assert.Equal(-1, MonsterSpellcasting.TargetModeOf(8));
    }

    [Fact]
    public void ThoughtsLikeCloudsShutsTheCasterDown() {
        Assert.False(MonsterSpellcasting.CanSelect(casterHasThoughtsLikeClouds: true));
        Assert.True(MonsterSpellcasting.CanSelect(casterHasThoughtsLikeClouds: false));
    }

    [Fact]
    public void TheScanStartsAtTheLastRealSpellNotPastIt() {
        // The original seeds with the count and so reads one record too far; we start at count - 1.
        Assert.Equal(44, MonsterSpellcasting.FirstCandidate(45));
        Assert.True(MonsterSpellcasting.ScanStartsPastTheEndOfTheTable);
    }

    [Fact]
    public void TwoSpellsAreStruckOutByNumber() {
        Assert.True(MonsterSpellcasting.NeverSelected(SpellIds.Invitation));
        Assert.True(MonsterSpellcasting.NeverSelected(SpellIds.ThoughtsLikeClouds));
        Assert.False(MonsterSpellcasting.NeverSelected(SpellIds.Skyfire));
    }

    [Fact]
    public void ACandidateMustSurviveEveryFilter() {
        Assert.True(MonsterSpellcasting.Selects(SpellIds.Skyfire, matchesFilters: true,
            castable: true, coinFlipHeads: true, alreadyOnTarget: false));
        Assert.False(MonsterSpellcasting.Selects(SpellIds.Skyfire, matchesFilters: false,
            castable: true, coinFlipHeads: true, alreadyOnTarget: false));
        Assert.False(MonsterSpellcasting.Selects(SpellIds.Skyfire, matchesFilters: true,
            castable: false, coinFlipHeads: true, alreadyOnTarget: false));
        Assert.False(MonsterSpellcasting.Selects(SpellIds.Skyfire, matchesFilters: true,
            castable: true, coinFlipHeads: false, alreadyOnTarget: false));
    }

    [Fact]
    public void AndIsRefusedWhenTheTargetAlreadyCarriesIt() {
        // The one place the engine consults the effect pool before casting — and it looks at the
        // target, not the caster.
        Assert.False(MonsterSpellcasting.Selects(SpellIds.Skyfire, matchesFilters: true,
            castable: true, coinFlipHeads: true, alreadyOnTarget: true));
    }

    [Fact]
    public void AnExcludedSpellIsRefusedEvenWhenEverythingElsePasses() {
        Assert.False(MonsterSpellcasting.Selects(SpellIds.Invitation, matchesFilters: true,
            castable: true, coinFlipHeads: true, alreadyOnTarget: false));
    }
}
