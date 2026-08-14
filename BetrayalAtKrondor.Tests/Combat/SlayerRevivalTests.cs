namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using GameData.Resources.World;
using Xunit;

/// <summary>
/// The fallen getting back up. The species change and the presence gate are the two rules that make
/// this encounter behave the way it does.
/// </summary>
public class SlayerRevivalTests {
    private const int Down = SlayerRevival.DownFlag;

    [Fact]
    public void AFallenNighthawkRisesAsABlackSlayer() {
        // Not a restore — a species change. What gets up is not what went down.
        Assert.Equal((int)CreatureType.BlackSlayer,
            SlayerRevival.TypeAfterRising((int)CreatureType.Nighthawk));
    }

    [Fact]
    public void ABlackSlayerRisesAsItself() {
        Assert.Equal((int)CreatureType.BlackSlayer,
            SlayerRevival.TypeAfterRising((int)CreatureType.BlackSlayer));
    }

    [Fact]
    public void TheChangeIsOneWay() {
        int once = SlayerRevival.TypeAfterRising((int)CreatureType.Nighthawk);

        Assert.Equal(once, SlayerRevival.TypeAfterRising(once));
    }

    [Fact]
    public void NothingRisesUnlessOneIsAlreadyInTheFight() {
        // An encounter with only Nighthawks in it never sees a single revival.
        Assert.False(SlayerRevival.SweepRuns(0));
        Assert.True(SlayerRevival.SweepRuns(1));
    }

    [Fact]
    public void OnlyTheTwoSpeciesAreEligible() {
        Assert.True(SlayerRevival.IsEligibleSpecies((int)CreatureType.BlackSlayer));
        Assert.True(SlayerRevival.IsEligibleSpecies((int)CreatureType.Nighthawk));
        Assert.False(SlayerRevival.IsEligibleSpecies((int)CreatureType.Rogue));
        Assert.False(SlayerRevival.IsEligibleSpecies((int)CreatureType.MoredhelWarrior));
    }

    [Fact]
    public void OnlyADownActorIsACandidate() {
        Assert.True(SlayerRevival.IsCandidate((int)CreatureType.Nighthawk, Down));
        Assert.False(SlayerRevival.IsCandidate((int)CreatureType.Nighthawk, 0));
    }

    [Fact]
    public void TheSecondFlagBarsAnOtherwiseEligibleActor() {
        Assert.False(SlayerRevival.IsCandidate((int)CreatureType.Nighthawk,
            Down | SlayerRevival.RevivalBarredFlag));
    }

    [Fact]
    public void TheCountdownMustReachZeroFirst() {
        Assert.False(SlayerRevival.RisesThisTick(1, 4));
        Assert.True(SlayerRevival.RisesThisTick(0, 4));
    }

    [Fact]
    public void AnActorOffTheGridCountsDownForeverAndNeverRises() {
        // The position test sits alongside the countdown rather than before it, so leaving the field
        // does not cancel the wait — it just never ends.
        Assert.False(SlayerRevival.RisesThisTick(0, SlayerRevival.OffGrid));
    }

    [Fact]
    public void ABlockedTileKeepsTheBodyDownWithoutCancellingIt() {
        // The countdown has already reached zero, so it is retried every tick until the tile clears.
        Assert.False(SlayerRevival.CanRiseOnTile(tileBlocked: true));
        Assert.True(SlayerRevival.CanRiseOnTile(tileBlocked: false));
    }

    [Fact]
    public void WhatGetsUpIsAtFullStrength() {
        Assert.True(SlayerRevival.RisesAtFullStrength);
    }

    [Fact]
    public void RisingLeavesAHazardOnTheTile() {
        Assert.Equal(9, SlayerRevival.RisenTileEffect);
        Assert.Equal(400, SlayerRevival.RisenTileEffectDuration);
    }
}
