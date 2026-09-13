namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Character;
using Xunit;

/// <summary>
/// The per-member half of <c>stat_party_all_above_pct</c>, pinned against the running original.
/// </summary>
public class UpkeepEngineRestTests {
    // Measured 2026-09-13 by calling party_allMembersAboveStatPercent inside the running game with
    // the chapter-1 party loaded (Locklear 100/100, Owyn 80/85, Gorath 125/125). The binding member
    // is Owyn, and the original answered 1 up to and including 95, then 0 from 96.
    [Theory]
    [InlineData(80, true)]
    [InlineData(90, true)]
    [InlineData(93, true)]
    [InlineData(94, true)]   // the ratio form says false here
    [InlineData(95, true)]   // and here
    [InlineData(96, false)]
    [InlineData(100, false)]
    public void OwynAt80Of85MatchesTheOriginal(int percent, bool expected) =>
        Assert.Equal(expected, UpkeepEngine.IsAbovePercent(80, 85, percent));

    [Fact]
    public void ExactlyThePercentageCounts() =>
        Assert.True(UpkeepEngine.IsAbovePercent(80, 100, 80));

    [Fact]
    public void AMemberWithNoPoolIsNotCounted() =>
        Assert.True(UpkeepEngine.IsAbovePercent(0, 0, 80));
}
