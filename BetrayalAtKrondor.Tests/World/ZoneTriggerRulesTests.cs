namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// The zone-boundary prompt (<c>zoneTrigger_phase1</c> @0x74a82). What carries: crossing is an
/// offer, and the offer is what gates the move.
/// </summary>
public class ZoneTriggerRulesTests {
    private const uint SomePrompt = 2700023;

    [Fact]
    public void AnsweringZeroCrosses() =>
        Assert.True(ZoneTriggerRules.CrossesAfterPrompt(SomePrompt, 0));

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(-1)]
    public void AnythingElseStaysPut(int answer) =>
        Assert.False(ZoneTriggerRules.CrossesAfterPrompt(SomePrompt, answer));

    [Fact]
    public void ABoundaryWithNoPromptIsInert() {
        // Reads backwards — a missing question looks like it should mean "just go" — but phase 1
        // leaves its proceed flag clear there, so nothing happens. Unreachable with shipped data
        // (all 39 records name a prompt); modelled so a mod that clears the field gets the
        // original's nothing rather than a silent teleport.
        Assert.False(ZoneTriggerRules.CanCross(0));
        Assert.False(ZoneTriggerRules.CrossesAfterPrompt(0, ZoneTriggerRules.ProceedResult));
    }

    [Fact]
    public void APromptedBoundaryCanCross() =>
        Assert.True(ZoneTriggerRules.CanCross(SomePrompt));

    [Fact]
    public void TheBooleanAndIntFormsAgree() {
        // The remake's confirm helper returns chosen==0 as a bool — the same polarity the boundary
        // wants. A helper that returned true for "No" would invert every border in the game and
        // nothing would look wrong, so the agreement is pinned rather than assumed.
        Assert.Equal(
            ZoneTriggerRules.CrossesAfterPrompt(SomePrompt, ZoneTriggerRules.ProceedResult),
            ZoneTriggerRules.CrossesAfterPrompt(SomePrompt, true));
        Assert.Equal(
            ZoneTriggerRules.CrossesAfterPrompt(SomePrompt, 1),
            ZoneTriggerRules.CrossesAfterPrompt(SomePrompt, false));
    }

    [Fact]
    public void APromptlessBoundaryIgnoresEvenAYes() =>
        Assert.False(ZoneTriggerRules.CrossesAfterPrompt(0, true));

    [Fact]
    public void TheArrivalMessageIsOptional() {
        Assert.False(ZoneTriggerRules.AnnouncesArrival(0));
        Assert.True(ZoneTriggerRules.AnnouncesArrival(1800001));
    }
}
