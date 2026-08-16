namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using Xunit;

/// <summary>
/// The GiveItem ids that mean money. Untested until 2026-08-16, which is how the party came to
/// start the game with nothing.
/// </summary>
public class DialogMoneyGrantTests {
    [Fact]
    public void SovereignsAndRoyalsAreMoney() {
        Assert.True(DialogMoneyGrant.IsMoney(DialogMoneyGrant.SovereignObjectId));
        Assert.True(DialogMoneyGrant.IsMoney(DialogMoneyGrant.RoyalObjectId));
    }

    [Fact]
    public void AnythingElseIsAnItem() {
        // 80 is Picklocks, 18 a Broadsword — both real items that need a pack, not a purse.
        Assert.False(DialogMoneyGrant.IsMoney(80));
        Assert.False(DialogMoneyGrant.IsMoney(18));
        Assert.False(DialogMoneyGrant.IsMoney(0));
    }

    [Fact]
    public void ASovereignIsTenRoyals() =>
        Assert.Equal(140, DialogMoneyGrant.RoyalsFor(DialogMoneyGrant.SovereignObjectId, 14));

    [Fact]
    public void ARoyalIsItself() =>
        Assert.Equal(3, DialogMoneyGrant.RoyalsFor(DialogMoneyGrant.RoyalObjectId, 3));

    [Fact]
    public void TheGameStartsWithFourteenSovereignsAndThreeRoyals() =>
        // 143 royals. The figure the original starts a new game with, and the one this class exists
        // to stop being dropped on the floor.
        Assert.Equal(143,
            DialogMoneyGrant.RoyalsFor(DialogMoneyGrant.SovereignObjectId, 14)
            + DialogMoneyGrant.RoyalsFor(DialogMoneyGrant.RoyalObjectId, 3));

    [Fact]
    public void AnItemIsWorthNoRoyals() =>
        // The caller must test IsMoney first; this returning 0 is a floor, not a licence to add it
        // to the purse for every item handed over.
        Assert.Equal(0, DialogMoneyGrant.RoyalsFor(80, 5));
}
