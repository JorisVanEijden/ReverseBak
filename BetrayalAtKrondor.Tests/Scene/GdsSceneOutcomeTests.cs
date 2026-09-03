namespace BetrayalAtKrondor.Tests.Scene;

using GameData.Resources.Scene;
using Xunit;

/// <summary>
/// A hotspot dialog's answer overriding which scene action runs.
/// </summary>
/// <remarks>
/// The table was modelled with nothing able to reach it until <c>SetReturnValue</c> was wired —
/// <c>ShowById</c> returned no value, so every dialog result was silently the default.
/// </remarks>
public class GdsSceneOutcomeTests {
    [Fact]
    public void TheFiveTranslatedResultsAreNeitherOrderedNorDerivable() {
        // Nothing to derive: -1 -> 0, -2 -> 5, -3 -> 7, -4 -> 3, -5 -> 10. A port that "simplified"
        // this into a negation or an offset would send four of the five to the wrong action.
        Assert.Equal(0, GdsSceneRules.OutcomeFor(-1, currentOutcome: 99));
        Assert.Equal(5, GdsSceneRules.OutcomeFor(-2, currentOutcome: 99));
        Assert.Equal(7, GdsSceneRules.OutcomeFor(-3, currentOutcome: 99));
        Assert.Equal(3, GdsSceneRules.OutcomeFor(-4, currentOutcome: 99));
        Assert.Equal(10, GdsSceneRules.OutcomeFor(-5, currentOutcome: 99));
    }

    [Fact]
    public void EverythingElseLeavesTheHotspotsOwnCodeAlone() {
        // Which is how a dialog that merely said something falls through — and 0 is what a dialog
        // with no SetReturnValue answers, so this arm is the overwhelmingly common one.
        Assert.Equal(99, GdsSceneRules.OutcomeFor(0, currentOutcome: 99));
        Assert.Equal(99, GdsSceneRules.OutcomeFor(-6, currentOutcome: 99));
        Assert.Equal(99, GdsSceneRules.OutcomeFor(4, currentOutcome: 99));
    }

    [Fact]
    public void OnlyTheMinusTwoArmInvalidatesThePalette() {
        Assert.True(GdsSceneRules.InvalidatesPalette(-2));
        foreach (int other in new[] { -1, -3, -4, -5, 0 }) {
            Assert.False(GdsSceneRules.InvalidatesPalette(other));
        }
    }
}
