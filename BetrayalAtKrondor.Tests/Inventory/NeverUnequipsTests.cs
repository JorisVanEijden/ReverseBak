namespace BetrayalAtKrondor.Tests.Inventory;

using GameData;
using GameData.Resources.Data;
using GameData.Resources.Inventory;
using Xunit;

/// <summary>
/// What refuses to leave a member because it is worn.
/// </summary>
/// <remarks>
/// Found on 2026-09-07 by dragging an equipped Broadsword onto the shop window in the ORIGINAL at
/// Fletcher's Post: it answers *"he realized it would be utter madness to strip himself of his
/// defenses"*. The port sold it — gold 143 -> 188 — because the sell path short-circuits ahead of
/// the shared transfer and never asked. The rule itself was already modelled; nothing called it.
/// </remarks>
public class NeverUnequipsTests {
    private const ushort Equipped = (ushort)ItemFlags.Equipped;

    [Theory]
    [InlineData(ObjectType.Sword, true)]
    [InlineData(ObjectType.Staff, true)]
    [InlineData(ObjectType.Crossbow, false)]  // category 2 — the original names 1 and 3 only
    [InlineData(ObjectType.Armor, false)]     // category 4 likewise
    public void OnlyASwordOrStaffRefusesToBeUnequipped(ObjectType category, bool refuses) {
        var worn = new RuntimeItem(18, 100, Equipped);

        Assert.Equal(refuses,
            InventoryTransfer.NeverUnequips(worn, category, SaveGameContainerType.Inventory));
    }

    [Fact]
    public void AnUNequippedSwordSellsFreely() {
        var spare = new RuntimeItem(18, 100, 0);

        Assert.False(InventoryTransfer.NeverUnequips(
            spare, ObjectType.Sword, SaveGameContainerType.Inventory));
    }

    [Fact]
    public void TheGuardIsOnTheSOURCEBeingAMembersPack() {
        // The flag can ride on an item sitting in a shop's own stock — we put it there ourselves by
        // selling a worn sword before this guard existed. Coming back OUT of the shop it must not
        // be treated as somebody's equipment.
        var worn = new RuntimeItem(18, 100, Equipped);

        Assert.False(InventoryTransfer.NeverUnequips(
            worn, ObjectType.Sword, SaveGameContainerType.FixedWorldItem));
    }
}
