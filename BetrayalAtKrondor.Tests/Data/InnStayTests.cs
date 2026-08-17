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
    public void TheStayEndsWhenTheCLOCKREACHESTheInnsWakingHour() =>
        Assert.True(InnStay.StayComplete(hourOfDay: 5, innWakeHour: 5));

    [Fact]
    public void TheStayIsNotOverBeforeThatHour() =>
        Assert.False(InnStay.StayComplete(4, 5));

    [Fact]
    public void ANightBookedInTheEVENINGRunsPastMidnight() {
        // The whole point of reading the byte as an hour of day. Booking at 20:00 with a waking
        // hour of 5 is a NINE-hour night; read as a duration it would end at 01:00 after five.
        var hour = 20;
        var hours = 0;
        while (!InnStay.StayComplete(hour, 5)) {
            hour = (hour + 1) % 24;
            hours++;
        }

        Assert.Equal(9, hours);
        Assert.Equal(5, hour);
    }

    [Fact]
    public void TheHourOfDayWrapsWithTheClock() {
        Assert.Equal(0, InnStay.HourOfDay(0));
        Assert.Equal(0, InnStay.HourOfDay(1799));
        Assert.Equal(1, InnStay.HourOfDay(1800));
        Assert.Equal(23, InnStay.HourOfDay(23 * 1800));
        Assert.Equal(0, InnStay.HourOfDay(24 * 1800));          // a new day, not hour 24
        Assert.Equal(5, InnStay.HourOfDay(3 * InnStay.TicksPerDay + 5 * 1800));
    }

    [Fact]
    public void AnInnRestsHarderThanACamp() {
        // Two effects from one figure, and the port has to pass the figure rather than the result:
        // gstate_hourly_tick reads exactly PartialRestQuality as "cap at 80%" and anything else as
        // "fill", and scales the hour's regeneration by quality/100.
        Assert.NotEqual(UpkeepEngine.PartialRestQuality, InnStay.RestQuality);
        Assert.Equal(133, InnStay.RestQuality);
        Assert.Equal(100, InnStay.RestedPercent);
    }

    [Fact]
    public void TheInnKeepsOfferingUntilThePartyIsWhole() {
        Assert.True(InnStay.OfferAnotherNight(everyMemberAtFullPool: false));
        Assert.False(InnStay.OfferAnotherNight(everyMemberAtFullPool: true));
    }
}
