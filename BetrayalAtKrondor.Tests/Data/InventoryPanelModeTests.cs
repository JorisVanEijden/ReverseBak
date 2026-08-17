namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Data;
using GameData.Resources.Inventory;
using Xunit;

/// <summary>Which inventory background is drawn (<c>UI_DrawInventory</c> @0x56880).</summary>
public class InventoryPanelModeTests {
    [Fact]
    public void AMembersOwnPackGetsTheSplitLayout() =>
        Assert.False(InventoryPanelMode.UsesWideBackground(
            SaveGameContainerType.Inventory, InventoryPanelMode.ShopMode.Off));

    [Theory]
    [InlineData(SaveGameContainerType.Corpse)]
    [InlineData(SaveGameContainerType.Chest)]
    [InlineData(SaveGameContainerType.Bag)]
    [InlineData(SaveGameContainerType.SharedKeys)]
    public void AnyOtherContainerGetsTheWideLootBackground(SaveGameContainerType type) =>
        Assert.True(InventoryPanelMode.UsesWideBackground(type, InventoryPanelMode.ShopMode.Off));

    [Fact]
    public void ShopModeForcesTheSplitLayoutEvenForANonMemberContainer() =>
        // THE CASE A CONTAINER-TYPE TEST ALONE GETS WRONG. The picklock screen shows a scratch
        // container typed SharedKeys with the flag SET; the original draws the narrow panel.
        Assert.False(InventoryPanelMode.UsesWideBackground(
            SaveGameContainerType.SharedKeys, InventoryPanelMode.ShopMode.On));

    [Fact]
    public void ShopModeChangesNothingForAMembersPack() =>
        // Both arms of the original's test lead to narrow, so the flag cannot make a member's pack
        // any narrower.
        Assert.False(InventoryPanelMode.UsesWideBackground(
            SaveGameContainerType.Inventory, InventoryPanelMode.ShopMode.On));

    [Fact]
    public void TheWideBackgroundNeedsBothConditions() {
        // Stated as the conjunction the original computes, so neither half can be dropped.
        foreach (SaveGameContainerType type in new[] {
                     SaveGameContainerType.Inventory, SaveGameContainerType.Corpse }) {
            foreach (InventoryPanelMode.ShopMode mode in new[] {
                         InventoryPanelMode.ShopMode.Off, InventoryPanelMode.ShopMode.On }) {
                bool expected = type != SaveGameContainerType.Inventory
                    && mode == InventoryPanelMode.ShopMode.Off;
                Assert.Equal(expected, InventoryPanelMode.UsesWideBackground(type, mode));
            }
        }
    }
}
