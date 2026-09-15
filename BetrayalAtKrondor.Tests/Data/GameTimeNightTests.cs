namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.GameState;
using Xunit;

/// <summary>
/// The dialog's night (globals 30009 / 30010): before 04:00 or from 20:00. TASK-549 moved the rule here
/// so a live session and a save read on its own ask the same question.
/// </summary>
public class GameTimeNightTests {
    private static int At(int day, int hour, int minute) =>
        (day * GameTime.UnitsPerDay) + (hour * GameTime.UnitsPerHour) + (minute * GameTime.UnitsPerHour / 60);

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(3, 59, true)]
    [InlineData(4, 0, false)]
    [InlineData(19, 59, false)]
    [InlineData(20, 0, true)]
    [InlineData(23, 59, true)]
    public void NightIsBeforeFourAndFromTwenty(int hour, int minute, bool night) =>
        Assert.Equal(night, GameTime.IsNight(At(6, hour, minute)));
}
