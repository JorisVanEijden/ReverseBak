namespace BetrayalAtKrondor.Tests.Character;

using GameData;
using GameData.Resources.Character;
using Xunit;

/// <summary>
/// The temple's weapon-blessing transaction. The replace-not-upgrade rule is the one a port gets
/// backwards, and it is the reason the screen asks before re-blessing.
/// </summary>
public class TempleBlessingTests {
    [Fact]
    public void OnlySwordsAndArmourCanBeBlessed() {
        Assert.True(TempleBlessing.CanBless(ObjectType.Sword));
        Assert.True(TempleBlessing.CanBless(ObjectType.Armor));

        // Not the other two wearables — a magician's staff can never be blessed, though the bonus
        // itself would apply to whatever is equipped.
        Assert.False(TempleBlessing.CanBless(ObjectType.Staff));
        Assert.False(TempleBlessing.CanBless(ObjectType.Crossbow));
        Assert.False(TempleBlessing.CanBless(ObjectType.Potion));
    }

    [Fact]
    public void ThePriceIsAPercentageOfBaseplusAFlatFeeInTens() {
        // 200 * 50 / 100 + 3 * 10
        Assert.Equal(130, TempleBlessing.Price(200, 50, 3));
    }

    [Fact]
    public void ThePercentagePartTruncatesBeforeTheFeeIsAdded() {
        // 7 * 50 / 100 = 3 (not 3.5), then + 10.
        Assert.Equal(13, TempleBlessing.Price(7, 50, 1));
    }

    [Fact]
    public void ATempleThatChargesNothingIsExpressible() {
        Assert.Equal(0, TempleBlessing.Price(500, 0, 0));
    }

    [Fact]
    public void EachTierSetsItsOwnFlag() {
        Assert.Equal(ItemFlags.Blessed1, TempleBlessing.Bless(0, 1));
        Assert.Equal(ItemFlags.Blessed2, TempleBlessing.Bless(0, 2));
        Assert.Equal(ItemFlags.Blessed3, TempleBlessing.Bless(0, 3));
    }

    [Fact]
    public void ALowerTierREPLACESAHigherOneRatherThanLeavingIt() {
        // Paying a tier-1 temple to bless a tier-3 sword makes it WORSE. The original clears all
        // three bits before setting the new one; OR-ing would turn every re-blessing into a free
        // upgrade.
        Assert.Equal(ItemFlags.Blessed1, TempleBlessing.Bless(ItemFlags.Blessed3, 1));
    }

    [Fact]
    public void BlessingLeavesTheItemsOtherFlagsAlone() {
        ItemFlags flags = ItemFlags.Equipped | ItemFlags.Flaming | ItemFlags.Blessed2;

        Assert.Equal(ItemFlags.Equipped | ItemFlags.Flaming | ItemFlags.Blessed3,
            TempleBlessing.Bless(flags, 3));
    }

    [Fact]
    public void ATierOutsideTheThreeLeavesTheItemUntouched() {
        Assert.Equal(ItemFlags.Blessed2, TempleBlessing.Bless(ItemFlags.Blessed2, 0));
        Assert.Equal(ItemFlags.Blessed2, TempleBlessing.Bless(ItemFlags.Blessed2, 4));
    }

    [Fact]
    public void AnyTierCountsAsAlreadyBlessed() {
        Assert.True(TempleBlessing.IsBlessed(ItemFlags.Blessed1));
        Assert.True(TempleBlessing.IsBlessed(ItemFlags.Blessed3));
        Assert.False(TempleBlessing.IsBlessed(ItemFlags.Equipped | ItemFlags.Enhanced2));
    }

    [Fact]
    public void TheOfferWordingDistinguishesArmourFromASword() {
        Assert.Equal(1, TempleBlessing.OfferWordingFor(ObjectType.Armor));
        Assert.Equal(0, TempleBlessing.OfferWordingFor(ObjectType.Sword));
    }
}
