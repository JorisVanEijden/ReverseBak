namespace ResourceExtraction.Tests.Layout;

using GameData.Resources.Inventory;
using GameData.Resources.Layout;
using GameData.Resources.Menu;

using ResourceExtraction.Extractors;

using Xunit;

/// <summary>Faithfulness gate for the inventory grid layout: these are exactly the constants
/// ItemGridRenderer.cs carried before the conversion (CellVgaW/H, OriginVgaX/Y, MemberShiftVgaX,
/// LootBoxVgaH). If one of these changes, the loot/inventory screen has moved relative to the
/// original.
///
/// <para>The four paperdoll slot rects and the loot box's width were dropped in task-54: they were
/// second encodings of geometry the renderer derives (from the grid, and from the design frame),
/// with no consumer. Dead data that looks authoritative is worse than absent data — a mod author
/// edits it, nothing moves, and the two encodings drift apart in the meantime.</para></summary>
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

        // Loot centering box: VGA height 132 -> 792. Only the VERTICAL axis is box-driven; the
        // horizontal one centres on the design frame (see the property doc and
        // ItemGridRenderer.ResolveGridOrigin), so a Width here would be data nothing applies.
        Assert.Equal(LayoutLength.Auto, layout.LootBox.Width);
        Assert.Equal(LayoutLength.Px(792f), layout.LootBox.Height);

        // The four paperdoll slot rects (Sword/Staff/Crossbow/Armor) used to be asserted here.
        // They were removed in task-54: exactly derivable from the grid above — column 0,
        // colspan 2, row+rowspan from ItemGridRenderer.TryPaperdollSlot — and only the derivation
        // was ever consumed. Two encodings of one geometry drift; the hints looked authoritative
        // to a mod author while nothing read them.
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

        // One original pixel, down-right (invui_draw_text_aligned_shadow). A design-frame px
        // scalar, not a LayoutLength — see the comment above the property.
        Assert.Equal(5f, layout.TextShadowOffsetX);
        Assert.Equal(6f, layout.TextShadowOffsetY);

        // One original pixel per axis: the icon flight's position lattice
        // (invinspect_animate_item_move @0x5A0CB steps in whole original pixels).
        Assert.Equal(5f, layout.IconFlightStepX);
        Assert.Equal(6f, layout.IconFlightStepY);

        // --- black item-panel fills (UI_DrawInventory @0x5687d) ---
        // paperdoll   VGA 13,11,82,121  -> 65,66,410,726
        AssertRect(layout.PaperdollBox, 65f, 66f, 410f, 726f);
        // general     VGA 105,11,202,121 -> 525,66,1010,726
        AssertRect(layout.GeneralItemsBox, 525f, 66f, 1010f, 726f);
        // continuous  VGA 13,11,294,121 -> 65,66,1470,726
        AssertRect(layout.FullItemsBox, 65f, 66f, 1470f, 726f);

        // --- empty paperdoll-slot silhouettes (UI_DrawInventory @0x56990) ---
        // One original pixel below the matching paperdoll cell's top edge: crossbow VGA (14,43),
        // armor VGA (14,73). That inset is the only number the model states — canonical 6 — because
        // the positions themselves are DERIVED from the grid by
        // ItemGridRenderer.PaperdollPlaceholderHint, so a resized grid takes its silhouettes with
        // it. The two properties are overrides, null unless a mod author pins a point.
        Assert.Equal(6f, layout.PaperdollPlaceholderNudgeY);
        Assert.Null(layout.CrossbowPlaceholder);
        Assert.Null(layout.ArmorPlaceholder);

        // --- "More Info" stat panel (UI_showItemStats @0x5A1DA) ---
        // The walk's two origins: general VGA (140,30) -> (700,180); the melee table shifts left
        // to fit two columns, VGA (115,30) -> (575,180).
        AssertPoint(layout.StatsOrigin, 700f, 180f);
        AssertPoint(layout.StatsWeaponOrigin, 575f, 180f);

        // Column offsets from the walk's x (horizontal, x5): +85 -> 425, +150 -> 750, +70 -> 350.
        Assert.Equal(425f, layout.StatsThrustColumn);
        Assert.Equal(750f, layout.StatsSwingColumn);
        Assert.Equal(350f, layout.StatsValueColumn);

        // Vertical advances (x6): row +10 -> 60, section gap +6 -> 36, melee header +15 -> 90,
        // underline +3 -> 18, staff nudge +10 -> 60, ranged block +25 -> 150, armor block
        // +15 -> 90. Named separately even where the numbers coincide — see the property comments.
        Assert.Equal(60f, layout.StatsLineAdvance);
        Assert.Equal(36f, layout.StatsSectionGap);
        Assert.Equal(90f, layout.StatsHeaderHeight);
        Assert.Equal(18f, layout.StatsUnderlineOffset);
        Assert.Equal(60f, layout.StatsStaffNudge);
        Assert.Equal(150f, layout.StatsCrossbowOffsetY);
        Assert.Equal(90f, layout.StatsArmorOffsetY);
        // Horizontal (x5): armor indents +15 -> 75.
        Assert.Equal(75f, layout.StatsArmorIndentX);

        // --- interaction ---
        // drag threshold ~4 original px x5 (sub_ovr158_1013 @0x57184)
        Assert.Equal(20f, layout.DragThreshold);
        // container-window hairline: one original px (sub_ovr158_3D0 @0x56420)
        Assert.Equal(5f, layout.ContainerBorderWidthX);
        Assert.Equal(6f, layout.ContainerBorderWidthY);
    }

    /// <summary>Two structural relationships between the fill boxes and the cell grid.
    ///
    /// <para>First: the grid sits <b>inside</b> the paperdoll fill, not flush with it — the box
    /// starts one original px left of and above the grid's first cell (VGA 13,11 vs 14,12). The
    /// name of this test used to claim the opposite ("share the grid area's top-left row"), which
    /// is exactly what the numbers below refute.</para>
    ///
    /// <para>Second: the paperdoll and general boxes are the split form of the continuous one, so
    /// they must keep its top, its height and its right edge (<c>UI_DrawInventory</c> @0x5687d
    /// paints whichever form the mode calls for, and the two forms cover the same area).</para>
    /// </summary>
    [Fact]
    public void FillBoxes_EncloseTheGrid_AndTheSplitFormCoversTheContinuousOne() {
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

    /// <summary>The silhouettes' positions are no longer stated, so what has to be pinned instead
    /// is that the grid they are derived FROM still produces the original's numbers: crossbow
    /// VGA (14,43) -> canonical (70,258), armor VGA (14,73) -> (70,438).
    ///
    /// <para>This restates <c>ItemGridRenderer.PaperdollPlaceholderHint</c>'s arithmetic (cell
    /// origin + nudge) deliberately — the renderer lives in the Unity assembly and cannot be
    /// referenced here, and the point of the check is that the shipped <see cref="InventoryLayout"/>
    /// data still feeds that arithmetic the right inputs. The rows come from
    /// <c>ItemGridRenderer.TryPaperdollSlot</c>: crossbow row 1, armor row 2.</para></summary>
    [Fact]
    public void GridAndNudge_DeriveTheOriginalsSilhouettePositions() {
        var layout = new InventoryLayout();
        float left = layout.GridArea.Left.Value;
        float top = layout.GridArea.Top.Value;
        float cellHeight = layout.GridArea.Grid!.CellHeight.Value;
        float nudge = layout.PaperdollPlaceholderNudgeY;

        Assert.Equal(70f, left);
        Assert.Equal(258f, top + (1 * cellHeight) + nudge); // crossbow, row 1
        Assert.Equal(438f, top + (2 * cellHeight) + nudge); // armor, row 2
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
