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

    /// <summary>Faithfulness gate for the named coordinates that used to be private VGA constants
    /// in <c>ItemInspectPanel</c>, <c>MemberEquip</c> and <c>InventoryMenu</c>. Every number is the
    /// original's own value scaled x5 horizontally / x6 vertically; the VGA source is spelled out
    /// beside each so a drift is visible without opening IDA.</summary>
    [Fact]
    public void Defaults_MatchTheOriginalNamedCoordinates() {
        var layout = new InventoryLayout();

        // --- item-inspect view (UI_showItem @0x5A778) ---
        // icon centre VGA (58,71) -> (290,426)
        AssertPoint(layout.InspectIcon, 290f, 426f);
        // name line 1 VGA (58,15) -> (290,90); line 2 VGA (58,25) -> (290,150)
        AssertPoint(layout.InspectNameFirstLine, 290f, 90f);
        AssertPoint(layout.InspectNameSecondLine, 290f, 150f);
        // type line VGA (58,35) -> (290,210)
        AssertPoint(layout.InspectTypeLine, 290f, 210f);
        // status line VGA (59,101) -> (295,606) — one original px right of the other three
        AssertPoint(layout.InspectStatusLine, 295f, 606f);

        // One original pixel, down-right (invui_draw_text_aligned_shadow).
        Assert.Equal(LayoutLength.Px(5f), layout.TextShadowOffsetX);
        Assert.Equal(LayoutLength.Px(6f), layout.TextShadowOffsetY);

        // One original pixel per axis: the icon flight's position lattice
        // (invinspect_animate_item_move @0x5A0CB steps in whole original pixels).
        Assert.Equal(LayoutLength.Px(5f), layout.IconFlightStepX);
        Assert.Equal(LayoutLength.Px(6f), layout.IconFlightStepY);

        // --- black item-panel fills (UI_DrawInventory @0x5687d) ---
        // paperdoll   VGA 13,11,82,121  -> 65,66,410,726
        AssertRect(layout.PaperdollBox, 65f, 66f, 410f, 726f);
        // general     VGA 105,11,202,121 -> 525,66,1010,726
        AssertRect(layout.GeneralItemsBox, 525f, 66f, 1010f, 726f);
        // continuous  VGA 13,11,294,121 -> 65,66,1470,726
        AssertRect(layout.FullItemsBox, 65f, 66f, 1470f, 726f);

        // --- empty paperdoll-slot silhouettes (UI_DrawInventory @0x56990) ---
        // crossbow VGA (14,43) -> (70,258); armor VGA (14,73) -> (70,438). Each sits one original
        // px below the matching paperdoll slot's top edge.
        AssertPoint(layout.CrossbowPlaceholder, 70f, 258f);
        AssertPoint(layout.ArmorPlaceholder, 70f, 438f);
        Assert.Equal(6f, layout.CrossbowPlaceholder.Top.Value - layout.CrossbowSlot.Top.Value);
        Assert.Equal(6f, layout.ArmorPlaceholder.Top.Value - layout.ArmorSlot.Top.Value);

        // --- interaction ---
        // drag threshold ~4 original px x5 (sub_ovr158_1013 @0x57184)
        Assert.Equal(LayoutLength.Px(20f), layout.DragThreshold);
        // container-window hairline: one original px (sub_ovr158_3D0 @0x56420)
        Assert.Equal(LayoutLength.Px(5f), layout.ContainerBorderWidthX);
        Assert.Equal(LayoutLength.Px(6f), layout.ContainerBorderWidthY);
    }

    /// <summary>The paperdoll drop target and the painted paperdoll fill are the SAME rect in the
    /// original (<c>invui_handle_item_drag</c> INVENTOR.C:609 vs <c>UI_DrawInventory</c> @0x5687d).
    /// They now share one property, so this pins the relationship the two used to restate
    /// separately in Unity code.</summary>
    [Fact]
    public void PaperdollBox_ShareTheGridAreasTopLeftRow() {
        var layout = new InventoryLayout();

        // The fill box starts one original px left of and one above the grid's first cell
        // (VGA 13,11 vs 14,12) — the cell grid sits inside the box, not flush with it.
        Assert.Equal(65f, layout.PaperdollBox.Left.Value);
        Assert.Equal(70f, layout.GridArea.Left.Value);
        Assert.Equal(66f, layout.PaperdollBox.Top.Value);
        Assert.Equal(72f, layout.GridArea.Top.Value);

        // Paperdoll + general boxes are the split form of the continuous one: same top and
        // height, and the general box ends where the continuous box does.
        Assert.Equal(layout.FullItemsBox.Top, layout.PaperdollBox.Top);
        Assert.Equal(layout.FullItemsBox.Height, layout.PaperdollBox.Height);
        Assert.Equal(layout.FullItemsBox.Top, layout.GeneralItemsBox.Top);
        Assert.Equal(layout.FullItemsBox.Height, layout.GeneralItemsBox.Height);
        Assert.Equal(layout.FullItemsBox.Left.Value + layout.FullItemsBox.Width.Value,
            layout.GeneralItemsBox.Left.Value + layout.GeneralItemsBox.Width.Value);
    }

    private static void AssertPoint(LayoutHint hint, float left, float top) {
        Assert.Equal(LayoutLength.Px(left), hint.Left);
        Assert.Equal(LayoutLength.Px(top), hint.Top);
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
