namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using System.Linq;
using Xunit;

/// <summary>
/// Whether a topic is on offer. The general rule is two flags; fifteen topics are not general at
/// all, which is the thing a data-only port cannot see.
/// </summary>
public class KeywordAvailabilityTests {
    [Fact]
    public void ATopicNeedsItsOwnFlagSet() {
        Assert.False(KeywordAvailability.IsAvailable(0, 0));
        Assert.True(KeywordAvailability.IsAvailable(1, 0));
    }

    [Fact]
    public void SuppressionHasTheLastWord() {
        // Applied after the hand-written cases, not instead of them — so a special condition saying
        // "yes" is still withdrawn when the topic is suppressed.
        Assert.False(KeywordAvailability.IsAvailable(1, 1));
    }

    [Fact]
    public void TheSuppressionFlagIsPerTopic() {
        Assert.Equal(6701, KeywordAvailability.SuppressedFlag(1));
        Assert.NotEqual(KeywordAvailability.SuppressedFlag(9),
            KeywordAvailability.SuppressedFlag(11));
    }

    [Fact]
    public void FifteenTopicsCarryConditionsTheDataDoesNotExpress() {
        Assert.Equal(15, KeywordAvailability.SpecialCases.Count);
    }

    [Fact]
    public void MostTopicsAreGovernedByTheGeneralRuleAlone() {
        Assert.False(KeywordAvailability.HasSpecialCase(1));
        Assert.Null(KeywordAvailability.SpecialCaseFor(1));
        Assert.True(KeywordAvailability.HasSpecialCase(11));
    }

    [Fact]
    public void SomeTopicsAreOfferedOnlyWhileYouStillNeedTheThing() {
        // Get the sense backwards and the topic appears exactly when it has stopped being useful.
        foreach (int key in new[] { 9, 11, 76, 148 }) {
            Assert.Equal(KeywordAvailability.Requirement.PartyLacksItem,
                KeywordAvailability.SpecialCaseFor(key)!.Value.Requirement);
        }
    }

    [Fact]
    public void TwoTopicsAskAboutOneNamedCharactersSpellbook() {
        // Not the party in general — a specific member, by name, in the executable.
        KeywordAvailability.SpecialCase first = KeywordAvailability.SpecialCaseFor(71)!.Value;
        KeywordAvailability.SpecialCase second = KeywordAvailability.SpecialCaseFor(106)!.Value;

        Assert.Equal(KeywordAvailability.Requirement.SpellNotKnown, first.Requirement);
        Assert.Equal(KeywordAvailability.Requirement.SpellNotKnown, second.Requirement);
        Assert.Equal("Owyn", first.Note);
        Assert.NotEqual(first.Second, second.Second);
    }

    [Fact]
    public void OneTopicIsGatedOnAParticularChapter() {
        KeywordAvailability.SpecialCase chapterGated = KeywordAvailability.SpecialCaseFor(117)!.Value;

        Assert.Equal(KeywordAvailability.Requirement.AtChapter, chapterGated.Requirement);
        Assert.Equal(6, chapterGated.First);
    }

    [Fact]
    public void TheExtraConditionNarrowsAvailabilityRatherThanGrantingIt() {
        // Twelve of the fifteen bail out first when the topic's own flag is clear.
        KeywordAvailability.SpecialCase narrowing = KeywordAvailability.SpecialCaseFor(44)!.Value;

        Assert.False(KeywordAvailability.ExtraConditionApplies(narrowing, 0));
        Assert.True(KeywordAvailability.ExtraConditionApplies(narrowing, 1));
    }

    [Fact]
    public void TheTwoRedirectsAreTheExceptionAndRunRegardless() {
        // They replace the value the general rule then tests, rather than testing alongside it.
        foreach (int key in new[] { 130, 133 }) {
            KeywordAvailability.SpecialCase redirect = KeywordAvailability.SpecialCaseFor(key)!.Value;

            Assert.Equal(KeywordAvailability.Requirement.FlagRedirect, redirect.Requirement);
            Assert.True(KeywordAvailability.ExtraConditionApplies(redirect, 0));
        }
    }

    [Fact]
    public void TwoTopicsShareAConditionRatherThanHavingOneEach() {
        // 17 and 103 ask the same question, as do 76 and 148 — the table is not one case per key.
        Assert.Equal(KeywordAvailability.SpecialCaseFor(17)!.Value.Requirement,
            KeywordAvailability.SpecialCaseFor(103)!.Value.Requirement);
        Assert.Equal(KeywordAvailability.SpecialCaseFor(17)!.Value.First,
            KeywordAvailability.SpecialCaseFor(103)!.Value.First);
    }

    [Fact]
    public void EveryRequirementKindInTheTableIsOneWeNamed() {
        Assert.All(KeywordAvailability.SpecialCases.Values,
            c => Assert.True(System.Enum.IsDefined(typeof(KeywordAvailability.Requirement), c.Requirement)));
        Assert.Contains(KeywordAvailability.SpecialCases.Values,
            c => c.Requirement == KeywordAvailability.Requirement.Unmodelled);
    }
}
