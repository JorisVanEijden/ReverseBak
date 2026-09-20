namespace BetrayalAtKrondor.Tests.Inventory;

using GameData;
using GameData.Resources.Inventory;
using GameData.Resources.Object;
using Xunit;

/// <summary>
/// A lit item's icon — and which lit items actually change picture.
/// </summary>
/// <remarks>
/// <c>invui_item_sprite_select</c> (canassa INVENTOR.C:100) has three runtime arms and only ONE of
/// them swaps the bitmap. Treating "lit" as a single rule would give the Ring of Prandur a torch's
/// treatment; it glows by palette instead (TASK-583).
/// </remarks>
public class LitItemIconTests {
    private static ObjectInfo Item(int number, int icon = 0) =>
        new ObjectInfo("test") { Number = number, Icon = icon };

    [Fact]
    public void ALitTorchIsADifferentBitmap() {
        // `if (item_id == 0x54 && (flags & 1)) pSprite = g_pInvSpriteHiAssetTable[8]` — the Hi
        // table IS INVSHP2, so this is #8 of that sheet and not 120 + 8.
        Assert.Equal("INVSHP2.BMX#8", ItemIconResolver.ResolveBmxSubResource(
            Item(InventoryConsume.TorchObjectId), (ushort)ItemFlags.Lit));
    }

    [Fact]
    public void AnUnlitTorchKeepsItsOrdinaryIcon() {
        // The control: without it the override could be firing for every torch.
        string unlit = ItemIconResolver.ResolveBmxSubResource(
            Item(InventoryConsume.TorchObjectId), 0);

        Assert.NotEqual("INVSHP2.BMX#8", unlit);
        Assert.Equal(ItemIconResolver.ResolveBmxSubResource(Item(InventoryConsume.TorchObjectId)),
            unlit);
    }

    [Fact]
    public void ALitRingOfPrandurDoesNOTChangeBitmap() {
        // Its arm shifts eight palette entries and leaves the sprite alone. A port that gave it a
        // second sprite would be inventing art the original does not have.
        const int ringOfPrandur = 6;

        Assert.Equal(ItemIconResolver.ResolveBmxSubResource(Item(ringOfPrandur), 0),
            ItemIconResolver.ResolveBmxSubResource(Item(ringOfPrandur), (ushort)ItemFlags.Lit));
    }

    [Theory]
    [InlineData(5, 0, "INVSHP1.BMX#5")]
    [InlineData(130, 0, "INVSHP2.BMX#10")]
    [InlineData(5, 121, "INVSHP2.BMX#1")]
    public void TheOrdinaryMappingIsUnchanged(int number, int icon, string expected) {
        // index = Icon != 0 ? Icon : Number, split at 120 — the flags overload must not disturb it.
        Assert.Equal(expected, ItemIconResolver.ResolveBmxSubResource(Item(number, icon), 0));
    }
}
