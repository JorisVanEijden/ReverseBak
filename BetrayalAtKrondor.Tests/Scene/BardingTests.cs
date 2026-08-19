namespace BetrayalAtKrondor.Tests.Scene;

using GameData.Resources.Scene;
using Xunit;

/// <summary>Playing the lute at a tavern (<c>container_PerformBarding</c> @0x4dd1e).</summary>
public class BardingTests {
    private const int Fund = 8;

    [Fact]
    public void ATavernWithNothingLeftSaysSoAndTheVisitGoesOn() {
        // "No money" and "no talent" are different answers: the tapped-out path returns the success
        // flag untouched, so the party is not walked out for asking.
        Assert.Equal(Barding.FundTappedOutDialog, Barding.DialogFor(0, difficulty: 10, partyBestBarding: 90));
        Assert.Equal(0, Barding.Reward(0, 10, 90));
        Assert.False(Barding.ThrownOut(0, 10, 90));
    }

    [Fact]
    public void PlayingWellPaysTheWholeFundAndPlayingJustWellEnoughPaysHalf() {
        // Capable at all is difficulty < skill; comfortably so is (difficulty + 100) / 2 <= skill.
        Assert.Equal(Fund * 10, Barding.Reward(Fund, difficulty: 20, partyBestBarding: 60));
        Assert.Equal(Barding.ExcellentDialog, Barding.DialogFor(Fund, 20, 60));

        Assert.Equal(Fund * 5, Barding.Reward(Fund, difficulty: 20, partyBestBarding: 25));
        Assert.Equal(Barding.DecentDialog, Barding.DialogFor(Fund, 20, 25));
    }

    [Fact]
    public void ADrunkCrowdPaysForPlayingTheyShouldNotHave() {
        // Outmatched, but not by much: still paid, a quarter of the tenfold purse.
        Assert.Equal(Fund * 10 / 4, Barding.Reward(Fund, difficulty: 40, partyBestBarding: 35));
        Assert.Equal(Barding.DrunkPatronsDialog, Barding.DialogFor(Fund, 40, 35));
        Assert.False(Barding.ThrownOut(Fund, 40, 35));
    }

    [Fact]
    public void BeingOutOfYourDepthEndsTheVisit() {
        Assert.Equal(0, Barding.Reward(Fund, difficulty: 80, partyBestBarding: 20));
        Assert.Equal(Barding.FailedDialog, Barding.DialogFor(Fund, 80, 20));
        Assert.True(Barding.ThrownOut(Fund, 80, 20));
        // And GdsActionDispatch turns that into the scene's own exit rather than a shrug.
        Assert.Equal(3, GdsActionDispatch.ActionAfterBarding(bardingSucceeded: false));
    }

    [Fact]
    public void EvenBeingThrownOutTeachesSomething() {
        // The experience is handed out before the outcome is known, and it goes to the whole party.
        Assert.Equal(Barding.ExperienceWhenOutmatched, Barding.ExperienceFor(Fund, 80, 20));
        Assert.Equal(Barding.ExperienceWhenCapable, Barding.ExperienceFor(Fund, 20, 60));
        // A tavern that cannot pay is not a performance at all.
        Assert.Equal(0, Barding.ExperienceFor(0, 20, 60));
    }

    [Fact]
    public void OnlyAPaidPerformanceSpendsTheFund() {
        // One paid performance per tavern, ever — but an unpaid one leaves it to try again.
        Assert.True(Barding.SpendsTheFund(Barding.Reward(Fund, 20, 60)));
        Assert.False(Barding.SpendsTheFund(Barding.Reward(Fund, 80, 20)));
    }

    [Fact]
    public void TheBestPlayerGetsADifferentTuneRatherThanTheSameOneBetter() {
        Assert.Equal(1008, Barding.SongFor(44));
        Assert.Equal(1040, Barding.SongFor(45));
        Assert.Equal(1039, Barding.SongFor(65));
        Assert.Equal(1007, Barding.SongFor(85));
    }

    [Fact]
    public void TheTiersAreNotCleanFractionsOfEachOther() {
        // The fund is multiplied by ten FIRST and the divisions truncate, so a port that reasoned
        // in halves and quarters of the payout would drift on odd funds.
        Assert.Equal(35, Barding.Reward(7, difficulty: 20, partyBestBarding: 25));
        Assert.Equal(17, Barding.Reward(7, difficulty: 40, partyBestBarding: 35));
    }
}
