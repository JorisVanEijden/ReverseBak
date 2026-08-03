namespace ResourceExtraction.Tests.Layout;

using GameData.Resources.Inventory;
using GameData.Resources.Layout;
using GameData.Resources.Menu;

using ResourceExtraction.Extractors;

using Xunit;

/// <summary>Faithfulness gate for the inventory grid layout: these are exactly the constants
/// ItemGridRenderer.cs carried before the conversion (CellVgaW/H, OriginVgaX/Y,
/// MemberShiftVgaX, LootBoxVgaW/H, and the four paperdoll rects from its class doc). If one of
/// these changes, the loot/inventory screen has moved relative to the original.</summary>
public class InventoryLayoutTests {
    [Fact]
    public void Defaults_MatchTheOriginalCanonicalGeometry() {
        var layout = new InventoryLayout();

        // Grid container: origin VGA (14,12) -> (70,72); cell 40x30 VGA -> 200x180; 7 cols x 4
        // rows (counts, not lengths -> unscaled).
        Assert.Equal(LayoutLength.Px(70f), layout.GridArea.Left);
        Assert.Equal(LayoutLength.Px(72f), layout.GridArea.Top);
        Assert.NotNull(layout.GridArea.Grid);
        Assert.Equal(LayoutLength.Px(200f), layout.GridArea.Grid.CellWidth);
        Assert.Equal(LayoutLength.Px(180f), layout.GridArea.Grid.CellHeight);
        Assert.Equal(7, layout.GridArea.Grid.Columns);
        Assert.Equal(4, layout.GridArea.Grid.Rows);

        // Member-mode shift: VGA 12 -> 60.
        Assert.Equal(LayoutLength.Px(60f), layout.MemberShiftX);

        // Loot centering box: VGA 307x132 -> 1535x792.
        Assert.Equal(LayoutLength.Px(1535f), layout.LootBox.Width);
        Assert.Equal(LayoutLength.Px(792f), layout.LootBox.Height);

        // Paperdoll rects (VGA -> canonical, x5/x6):
        // Sword    14,12,80,30 -> 70,72,400,180
        AssertRect(layout.SwordSlot, 70f, 72f, 400f, 180f);
        // Staff    14,12,80,60 -> 70,72,400,360
        AssertRect(layout.StaffSlot, 70f, 72f, 400f, 360f);
        // Crossbow 14,42,80,30 -> 70,252,400,180
        AssertRect(layout.CrossbowSlot, 70f, 252f, 400f, 180f);
        // Armor    14,72,80,60 -> 70,432,400,360
        AssertRect(layout.ArmorSlot, 70f, 432f, 400f, 360f);
    }

    private static void AssertRect(LayoutHint hint, float left, float top, float width, float height) {
        Assert.Equal(LayoutLength.Px(left), hint.Left);
        Assert.Equal(LayoutLength.Px(top), hint.Top);
        Assert.Equal(LayoutLength.Px(width), hint.Width);
        Assert.Equal(LayoutLength.Px(height), hint.Height);
    }

    [Fact]
    public void UserInterfaceExtractor_AttachesInventoryLayoutOnlyForReqInv() {
        UserInterface reqInv = ExtractMinimal("REQ_INV.DAT");
        Assert.NotNull(reqInv.Inventory);
        Assert.Equal(LayoutLength.Px(70f), reqInv.Inventory.GridArea.Left);

        // REQ_INV2 is a distinct screen (the shop/trade grid) and must NOT pick up this layout —
        // an id.Contains("REQ_INV") substring check would wrongly match it, so this guards the
        // exact-filename comparison in the extractor.
        UserInterface reqInv2 = ExtractMinimal("REQ_INV2.DAT");
        Assert.Null(reqInv2.Inventory);

        UserInterface reqMain = ExtractMinimal("REQ_MAIN.DAT");
        Assert.Null(reqMain.Inventory);
    }

    // Minimal well-formed REQ_*.DAT: header only, zero elements, no strings — enough for
    // UserInterfaceExtractor to run to completion and hit the id-dispatch hooks.
    private static UserInterface ExtractMinimal(string id) {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true)) {
            writer.Write((ushort)0);   // UserInterfaceType
            writer.Write((ushort)0);   // IsModal
            writer.Write((ushort)0);   // ColorBase
            writer.Write((ushort)0);   // XPosition
            writer.Write((ushort)0);   // YPosition
            writer.Write((ushort)0);   // Width
            writer.Write((ushort)0);   // Height
            writer.Write((ushort)0);   // entry count placeholder
            writer.Write((ushort)0);   // entry pointer placeholder
            writer.Write((short)-1);   // titleOffset (none)
            writer.Write((short)0);    // XOffset
            writer.Write((short)0);    // YOffset
            writer.Write((uint)0);     // bitmap pointer placeholder
            writer.Write((ushort)0);   // numberOfElements
            writer.Write((ushort)0);   // labelBufferSize
        }
        stream.Position = 0;
        return new UserInterfaceExtractor().Extract(id, stream);
    }
}
