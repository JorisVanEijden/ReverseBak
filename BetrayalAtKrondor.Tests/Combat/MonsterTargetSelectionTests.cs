namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// combat_selectTargetByMode — the seven criteria, the nearest-wins tie-break, and the exclusion
/// radius that explains why the casting factor shrinks as skill rises.
/// </summary>
public class MonsterTargetSelectionTests {
    [Fact]
    public void TheSixCasterSlotsCoverModesZeroToFive() {
        // Mode 6 exists but no action slot asks for it.
        var reached = new bool[7];
        for (int slot = 2; slot <= 7; slot++) {
            reached[MonsterSpellcasting.TargetModeOf(slot)] = true;
        }

        for (var mode = 0; mode <= 5; mode++) {
            Assert.True(reached[mode]);
        }
        Assert.False(reached[6]);
    }

    [Fact]
    public void ModesFourAndSixAreAMatchedPair() {
        // Both need the candidate to have a target; they differ only on whether that target can
        // still act.
        Assert.Equal(MonsterSpellcasting.TargetCriterion.EngagingSomeoneStillFighting,
            MonsterSpellcasting.CriterionOf(4));
        Assert.Equal(MonsterSpellcasting.TargetCriterion.EngagingSomeoneIncapacitated,
            MonsterSpellcasting.CriterionOf(6));
    }

    [Fact]
    public void TheOtherCriteriaAreWhatTheySay() {
        Assert.Equal(MonsterSpellcasting.TargetCriterion.Anyone, MonsterSpellcasting.CriterionOf(0));
        Assert.Equal(MonsterSpellcasting.TargetCriterion.Spellcaster,
            MonsterSpellcasting.CriterionOf(1));
        Assert.Equal(MonsterSpellcasting.TargetCriterion.Winded, MonsterSpellcasting.CriterionOf(2));
        Assert.Equal(MonsterSpellcasting.TargetCriterion.Archer, MonsterSpellcasting.CriterionOf(3));
        Assert.Equal(MonsterSpellcasting.TargetCriterion.EngagingTheFavouredActor,
            MonsterSpellcasting.CriterionOf(5));
    }

    [Fact]
    public void AnIncapacitatedCandidateIsSkippedEvenByModeZero() {
        // The test sits ahead of the mode switch, so "anyone" does not mean anyone.
        Assert.False(MonsterSpellcasting.CandidateIsEligible(candidateIncapacitated: true));
        Assert.True(MonsterSpellcasting.CandidateIsEligible(candidateIncapacitated: false));
    }

    [Fact]
    public void AnUnskilledCasterRefusesACrowdedTarget() {
        Assert.True(MonsterSpellcasting.CandidateIsTooCrowded(
            MonsterSpellcasting.CastingFactor(0), othersWithinRadius: 1));
    }

    [Fact]
    public void AndAMaximallySkilledOneRefusesNobody() {
        // Factor 0 short-circuits the counter before it looks at anyone.
        Assert.Equal(0, MonsterSpellcasting.CastingFactor(100));
        Assert.False(MonsterSpellcasting.CandidateIsTooCrowded(
            MonsterSpellcasting.CastingFactor(100), othersWithinRadius: 5));
    }

    [Fact]
    public void SoSkillWidensTheTargetSetRatherThanNarrowingIt() {
        // Which is what the seemingly inverted casting factor is for.
        const int crowd = 1;
        Assert.True(MonsterSpellcasting.CandidateIsTooCrowded(
            MonsterSpellcasting.CastingFactor(0), crowd));
        Assert.False(MonsterSpellcasting.CandidateIsTooCrowded(
            MonsterSpellcasting.CastingFactor(100), crowd));
    }

    [Fact]
    public void AnEmptyNeighbourhoodIsNeverCrowded() {
        Assert.False(MonsterSpellcasting.CandidateIsTooCrowded(
            MonsterSpellcasting.CastingFactor(0), othersWithinRadius: 0));
    }

    [Fact]
    public void TiesGoToTheLaterCandidate() {
        Assert.True(MonsterSpellcasting.NearestWinsAndTiesGoToTheLater);
    }
}
