namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// How a fight opens — <c>combTrigger_phase2</c>'s surprise block.
/// </summary>
public class CombatEncounterOpeningTests {
    [Fact]
    public void ThirtyMinutesExactlyStillCounts() {
        const long visited = 1000;
        Assert.True(CombatEncounterOpening.WasRecentlyVisited(visited + (30 * 30), visited));
        Assert.False(CombatEncounterOpening.WasRecentlyVisited(visited + (30 * 30) + 30, visited));
    }

    [Fact]
    public void AnUNVISITEDEncounterIsRecentForTheFirstHalfHourOfTheGame() {
        // The slot reads zero, so the elapsed time IS the clock. Early in chapter 1 every encounter
        // passes the recency test — the original's behaviour, not a rounding artefact.
        Assert.True(CombatEncounterOpening.WasRecentlyVisited(gameTime: 500, visitedTime: 0));
        Assert.False(CombatEncounterOpening.WasRecentlyVisited(gameTime: 5000, visitedTime: 0));
    }

    [Fact]
    public void AVisitStampedInTheFUTUREReadsAsLongAgoNotVeryRecent() {
        // The original divides unsigned, so the wrapped subtraction is enormous. A hand-edited save
        // is the only way to produce it, but it must not read as "just visited".
        Assert.False(CombatEncounterOpening.WasRecentlyVisited(gameTime: 100, visitedTime: 5000));
    }

    [Fact]
    public void NotRecentSkipsTheRollEntirely() {
        // A roll that would easily have succeeded changes nothing when the party has not been here.
        Assert.Equal(CombatEncounterOpening.Opening.NotRecent,
            CombatEncounterOpening.Resolve(recentlyVisited: false, rollUnder100: 0,
                bestPartyStealth: 99));
    }

    [Fact]
    public void TheSurpriseRollUsesTheRAWStatWithNoBonus() {
        // The avoidance roll in the same function gives a stat of 40 a chance of 52. This one does
        // not: 45 beats 40 here and would have failed there.
        Assert.Equal(52, CombatEncounterAvoidance.Chance(40, avoidable: false, dragonsBreathActive: false));
        Assert.Equal(CombatEncounterOpening.Opening.NoSurprise,
            CombatEncounterOpening.Resolve(recentlyVisited: true, rollUnder100: 45,
                bestPartyStealth: 40));
    }

    [Fact]
    public void ARollEqualToTheStatSurprises() {
        Assert.Equal(CombatEncounterOpening.Opening.PartySurprises,
            CombatEncounterOpening.Resolve(recentlyVisited: true, rollUnder100: 40,
                bestPartyStealth: 40));
    }

    [Fact]
    public void ONLYTheSurpriseGivesTheArenaTheAdvantage() {
        Assert.True(CombatEncounterOpening.PartyHasTheDrop(
            CombatEncounterOpening.Opening.PartySurprises));
        Assert.False(CombatEncounterOpening.PartyHasTheDrop(
            CombatEncounterOpening.Opening.NoSurprise));
        Assert.False(CombatEncounterOpening.PartyHasTheDrop(
            CombatEncounterOpening.Opening.NotRecent));
    }

    [Fact]
    public void TheThreeOpeningsKeepTheOriginalsNumbering() {
        // The dialog branches on these values, so they are data rather than an internal enum.
        Assert.Equal(0, (int)CombatEncounterOpening.Opening.PartySurprises);
        Assert.Equal(1, (int)CombatEncounterOpening.Opening.NoSurprise);
        Assert.Equal(2, (int)CombatEncounterOpening.Opening.NotRecent);
    }
}
