namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using Xunit;

/// <summary>
/// Whether a location's keeper is called a tavernkeeper or a shopkeeper — the engine's
/// <c>shopOrTavern</c>, written in <c>sub_ovr157_0</c> @0x5426e.
/// </summary>
public class DialogKeeperWordTests {
    [Fact]
    public void ANightlyChargeMakesATavernkeeper() =>
        Assert.True(DialogSlotContext.RunsAnInn(innCostPerNight: 5));

    [Fact]
    public void ChargingNothingIsAShop() =>
        Assert.False(DialogSlotContext.RunsAnInn(0));

    [Fact]
    public void NoShopBlockAtAllIsAShop() =>
        // The original clears the flag first and skips the test when the block is absent, so
        // "no data" and "free" give the same answer rather than the absent case being undefined.
        Assert.False(DialogSlotContext.RunsAnInn(null));

    [Fact]
    public void TheCostIsTheDiscriminator_NotTheRestHours() {
        // sub_ovr157_0 tests innCostPerNight and nothing else. A location that states hours but
        // charges nothing is a SHOP — the sort of thing only the disassembly settles, and the
        // reason "set from the container's nightly rate" was not specific enough to implement.
        Assert.False(DialogSlotContext.RunsAnInn(0));
        Assert.True(DialogSlotContext.RunsAnInn(1));
    }

    [Fact]
    public void TheFlagDrivesTheWordKind28Prints() {
        var shop = new DialogSlotContext { IsRestEncounter = DialogSlotContext.RunsAnInn(0) };
        var inn = new DialogSlotContext { IsRestEncounter = DialogSlotContext.RunsAnInn(12) };

        Assert.False(shop.IsRestEncounter);
        Assert.True(inn.IsRestEncounter);
    }
}
