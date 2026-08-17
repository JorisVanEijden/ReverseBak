namespace BetrayalAtKrondor.Tests.Data;

using GameData.Money;
using GameData.Resources.Character;
using Xunit;

/// <summary>The paid overnight stay (<c>UI_RestUntilTime</c> @0x4ff5c).</summary>
public class InnStayTests {
    [Fact]
    public void TheStoredRateIsInSovereigns() =>
        // x10 before printing AND before deducting, and ten royals make a sovereign — a unit
        // conversion, not a scale factor. Spending the byte directly undercharges tenfold.
        Assert.Equal(30 * MoneyFormatter.RoyalsPerSovereign, InnStay.CostInRoyals(30));

    [Fact]
    public void AFreeInnCostsNothing() =>
        Assert.Equal(0, InnStay.CostInRoyals(0));

    [Fact]
    public void ThePriceMatchesWhatTheOfferPrints() {
        // Both the printed price (0x501d3) and the deduction (0x5034c) apply the same x10, so the
        // player is never quoted one number and charged another.
        const int rate = 7;
        Assert.Equal(InnStay.CostInRoyals(rate), rate * MoneyFormatter.RoyalsPerSovereign);
    }

    [Fact]
    public void TheStayEndsOnTheStatedHour() =>
        Assert.True(InnStay.StayComplete(hoursRested: 8, innRestHours: 8));

    [Fact]
    public void TheStayIsNotOverEarly() =>
        Assert.False(InnStay.StayComplete(7, 8));

    [Fact]
    public void OvershootingDoesNotCountAsComplete() =>
        // Exact equality, faithfully. A >= reading behaves the same while the rest loop is the only
        // thing advancing the clock, and diverges as soon as anything else can — the party would
        // sail past the end and never be charged or healed.
        Assert.False(InnStay.StayComplete(9, 8));

    [Fact]
    public void HoursComeFromTheGameClocksOwnUnits() {
        Assert.Equal(0, InnStay.HoursFrom(1799));
        Assert.Equal(1, InnStay.HoursFrom(1800));
        Assert.Equal(8, InnStay.HoursFrom(8 * 1800));
    }

    [Fact]
    public void AnInnRestsHarderThanACamp() =>
        // The mechanical difference the price buys: camp passes 80, the inn passes 100.
        Assert.Equal(100, InnStay.RestedPercent);
}
