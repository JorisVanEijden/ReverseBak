namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Config;
using Xunit;

/// <summary>
/// How long a dialog holds itself up, from the text-speed preference
/// (<c>dialog_input_wait_timed</c>, DIALOG.C:224-240).
/// </summary>
public class DialogTextSpeedTests {
    [Fact]
    public void SlowMeansWaitForThePlayer_NotAVeryLongTimeout() {
        // The engine sets an infinite deadline at speed 0 and only leaves on input.
        Assert.Null(DialogTextSpeed.AutoDismissSeconds(500, TextSpeed.Slow));
    }

    [Fact]
    public void FasterSettingsHoldTheTextForLess() {
        double? medium = DialogTextSpeed.AutoDismissSeconds(100, TextSpeed.Medium);
        double? fast = DialogTextSpeed.AutoDismissSeconds(100, TextSpeed.Fast);

        Assert.NotNull(medium);
        Assert.NotNull(fast);
        Assert.True(fast < medium);
    }

    [Fact]
    public void MoreTextHoldsForLonger() {
        double? shortLine = DialogTextSpeed.AutoDismissSeconds(20, TextSpeed.Medium);
        double? paragraph = DialogTextSpeed.AutoDismissSeconds(400, TextSpeed.Medium);

        Assert.True(paragraph > shortLine);
    }

    [Theory]
    [InlineData(0, TextSpeed.Medium, 112)]   // (0*12 + 150) * 75 / 100
    [InlineData(100, TextSpeed.Medium, 1012)] // (100*12 + 150) * 75 / 100
    [InlineData(100, TextSpeed.Fast, 675)]    // (100*12 + 150) * 50 / 100
    [InlineData(100, TextSpeed.Slow, 1350)]   // scale 100 — the raw reading time
    public void TheTickFormulaIsTheEnginesOwn(int characters, TextSpeed speed, long expected) {
        Assert.Equal(expected, DialogTextSpeed.AutoDismissTicks(characters, speed));
    }

    [Fact]
    public void ATypicalLineReadsInAFewSeconds() {
        // 236.7 Hz, so a 100-character line is about 4.3s at Medium and 2.9s at Fast. If this ever
        // comes out in minutes, the tick rate is wrong — that is the number worth guarding.
        double medium = DialogTextSpeed.AutoDismissSeconds(100, TextSpeed.Medium)!.Value;
        double fast = DialogTextSpeed.AutoDismissSeconds(100, TextSpeed.Fast)!.Value;

        Assert.InRange(medium, 4.0, 4.6);
        Assert.InRange(fast, 2.6, 3.1);
    }

    [Fact]
    public void EvenAnEmptyLineGetsAMoment() {
        // The +150 floor: a two-word answer must not vanish the instant it appears.
        Assert.True(DialogTextSpeed.AutoDismissSeconds(0, TextSpeed.Fast) > 0.2);
    }

    [Fact]
    public void NegativeLengthsAreTreatedAsEmpty() {
        Assert.Equal(
            DialogTextSpeed.AutoDismissTicks(0, TextSpeed.Fast),
            DialogTextSpeed.AutoDismissTicks(-5, TextSpeed.Fast));
    }
}
