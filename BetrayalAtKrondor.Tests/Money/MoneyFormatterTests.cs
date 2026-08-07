namespace BetrayalAtKrondor.Tests.Money;

using GameData.Money;
using Xunit;

/// <summary>
/// Expectations transcribed from <c>FormatMoneyToString</c> @0x42d9a and the inventory readout at
/// @0x56dd0 (spec: docs/specs/party-money-display.md §2, §3.1). Amounts are ROYALS.
/// </summary>
[Collection(BetrayalAtKrondor.Tests.Text.UiStringsCollection.Name)]
public class MoneyFormatterTests {
    // ---- the split -----------------------------------------------------------------------

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(5, 0, 5)]
    [InlineData(10, 1, 0)]
    [InlineData(1234, 123, 4)]
    [InlineData(-15, -1, -5)] // signed idiv: both parts carry the sign, nothing clamps
    public void SplitsIntoSovereignsAndRoyals(int amount, int sovereigns, int royals) {
        Assert.Equal(sovereigns, MoneyFormatter.Sovereigns(amount));
        Assert.Equal(royals, MoneyFormatter.Royals(amount));
    }

    // ---- currency_sovereigns_royals (dialog prose) ---------------------------------------

    [Theory]
    [InlineData(0, "0 sovereign")]     // the %c-writes-NUL quirk: singular, even at zero
    [InlineData(10, "1 sovereign")]
    [InlineData(20, "2 sovereigns")]
    [InlineData(1, "1 royal")]
    [InlineData(5, "5 royals")]
    [InlineData(15, "1 sovereign and 5 royals")]
    [InlineData(21, "2 sovereigns and 1 royal")]
    [InlineData(25, "2 sovereigns and 5 royals")]
    [InlineData(11, "1 sovereign and 1 royal")]
    [InlineData(12345, "1234 sovereigns and 5 royals")]
    public void FormatsSovereignsAndRoyals(int amount, string expected) =>
        Assert.Equal(expected, MoneyFormatter.Format(amount, CurrencyStyle.SovereignsAndRoyals));

    [Fact]
    public void NegativeAmountKeepsTheSingularRoyal() =>
        // -5 is not > 1, so the trailing "s" is never appended — the original's own output.
        Assert.Equal("-1 sovereign and -5 royal",
            MoneyFormatter.Format(-15, CurrencyStyle.SovereignsAndRoyals));

    // ---- currency_gold_silver (inn screen, shop prices) ----------------------------------

    [Theory]
    [InlineData(0, "0 gold")]
    [InlineData(10, "1 gold")]
    [InlineData(20, "2 gold")]     // no pluralisation at all in this wording
    [InlineData(5, "5 silver")]    // zero gold drops the gold part entirely
    [InlineData(1, "1 silver")]
    [InlineData(15, "1 gold 5 silver")]
    [InlineData(1234, "123 gold 4 silver")]
    public void FormatsGoldAndSilver(int amount, string expected) =>
        Assert.Equal(expected, MoneyFormatter.Format(amount, CurrencyStyle.GoldAndSilver));

    // ---- the inventory readout ------------------------------------------------------------

    [Theory]
    [InlineData(0, "0s 0r")]        // an empty purse still prints both parts
    [InlineData(4, "0s 4r")]
    [InlineData(1234, "123s 4r")]
    [InlineData(99999, "9999s 9r")] // 9999 sovereigns: still under the limit
    public void FormatsAbbreviated(int amount, string expected) =>
        Assert.Equal(expected, MoneyFormatter.Format(amount, CurrencyStyle.Abbreviated));

    [Theory]
    [InlineData(100000, "10000")]   // 10000 sovereigns: royals AND unit letters are dropped
    [InlineData(100009, "10000")]
    public void AbbreviatedCollapsesPastTheLimit(int amount, string expected) =>
        Assert.Equal(expected, MoneyFormatter.Format(amount, CurrencyStyle.Abbreviated));
}
