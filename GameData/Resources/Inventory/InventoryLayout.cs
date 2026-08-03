namespace GameData.Resources.Inventory;

using GameData.Resources.Layout;

/// <summary>
/// Geometry of the loot/inventory grid screen (<c>REQ_INV</c>), in design-frame (1600x1200) px.
///
/// <para>None of these values are read from REQ_INV.DAT — they are immediate operands in the
/// layout builder <c>sub_ovr157_0</c> (KRONDOR.EXE 0x54210), exactly like the credits screen's
/// geometry (<see cref="Credits.CreditsLayout"/>), so they are transcribed here rather than
/// parsed. Converted from VGA once (x5 horizontal / x6 vertical, see AspectCorrection) so the
/// renderer needs no knowledge of the original 320x200 display.</para>
///
/// <para>Expressed as <see cref="LayoutHint"/> boxes (with a <see cref="LayoutGrid"/> for the
/// fixed-cell grid container) rather than loose named coordinates, so <c>LayoutApplier.Apply</c>
/// can consume them directly and an override can reflow the screen with percentages.</para>
///
/// <para>The defaults are the faithful values: an override that omits a property still gets the
/// original geometry.</para>
/// </summary>
public class InventoryLayout {
    /// <summary>The 7-column x 4-row cell grid container. Origin VGA (14,12) -> canonical
    /// (70,72); one cell 40x30 VGA -> 200x180 canonical (<c>sub_ovr157_0</c> @0x54210). Columns
    /// and rows are cell COUNTS, not lengths, so they are not VGA-scaled.</summary>
    public LayoutHint GridArea { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(70f),
        Top = LayoutLength.Px(72f),
        Grid = new LayoutGrid {
            CellWidth = LayoutLength.Px(200f),
            CellHeight = LayoutLength.Px(180f),
            Columns = 7,
            Rows = 4,
        },
    };

    /// <summary>Member/shop mode: every general (non-equipped) item is nudged this far right,
    /// into the right-hand panel box, clear of the paperdoll's reserved columns. VGA 12 ->
    /// canonical 60 (the member shift, <c>sub_ovr157_0</c> @0x545f3).</summary>
    public LayoutLength MemberShiftX { get; set; } = LayoutLength.Px(60f);

    /// <summary>Loot mode's centering box: the packed cluster of items (unshifted, unlike member
    /// mode) is centered inside this box after packing — the box itself is never drawn. VGA
    /// 307x132 -> canonical 1535x792, anchored at the screen's own origin (the loot centering
    /// pass, <c>sub_ovr157_0</c> @0x54663).</summary>
    public LayoutHint LootBox { get; set; } = new LayoutHint {
        Width = LayoutLength.Px(1535f),
        Height = LayoutLength.Px(792f),
    };

    /// <summary>Paperdoll rect for an equipped sword: one grid row tall. VGA (14,12,80,30) ->
    /// canonical (70,72,400,180) (the paperdoll blacking pass, <c>sub_ovr157_0</c> @0x569e4;
    /// slot selection @0x5451e). Shares row 0 with <see cref="StaffSlot"/> — a member carries
    /// one or the other.</summary>
    public LayoutHint SwordSlot { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(70f),
        Top = LayoutLength.Px(72f),
        Width = LayoutLength.Px(400f),
        Height = LayoutLength.Px(180f),
    };

    /// <summary>Paperdoll rect for an equipped staff: two grid rows tall (row 0). VGA
    /// (14,12,80,60) -> canonical (70,72,400,360) (<c>sub_ovr157_0</c> @0x569e4, @0x5451e).</summary>
    public LayoutHint StaffSlot { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(70f),
        Top = LayoutLength.Px(72f),
        Width = LayoutLength.Px(400f),
        Height = LayoutLength.Px(360f),
    };

    /// <summary>Paperdoll rect for an equipped crossbow: one grid row tall (row 1). VGA
    /// (14,42,80,30) -> canonical (70,252,400,180) (<c>sub_ovr157_0</c> @0x569e4, @0x5451e).</summary>
    public LayoutHint CrossbowSlot { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(70f),
        Top = LayoutLength.Px(252f),
        Width = LayoutLength.Px(400f),
        Height = LayoutLength.Px(180f),
    };

    /// <summary>Paperdoll rect for equipped armor: two grid rows tall (row 2). VGA (14,72,80,60)
    /// -> canonical (70,432,400,360) (<c>sub_ovr157_0</c> @0x569e4, @0x5451e).</summary>
    public LayoutHint ArmorSlot { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(70f),
        Top = LayoutLength.Px(432f),
        Width = LayoutLength.Px(400f),
        Height = LayoutLength.Px(360f),
    };
}
