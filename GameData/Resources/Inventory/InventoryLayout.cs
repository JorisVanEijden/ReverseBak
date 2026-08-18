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
/// original geometry. A few of those faithful values are <b>derived rather than stated</b> — the
/// empty-slot silhouettes come from <see cref="GridArea"/>, and the loot cluster's horizontal
/// centring from the design frame. Nothing here restates a geometry something else derives: two
/// encodings of one rect drift, and the dead one reads as authoritative to whoever edits it.</para>
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

    /// <summary>
    /// The shop shelf's own grid: 3 columns x 2 rows of 98x60 VGA cells from VGA (13,11) ->
    /// canonical (65,66), cells 490x360 (<c>sub_ovr157_0</c> @0x542e1-0x54319).
    /// </summary>
    /// <remarks>
    /// <b>A shop shows exactly SIX items and pages; it does not reuse the member grid.</b> The
    /// builder runs a separate arm for a container carrying a shop block, with its own constants
    /// and a hard <c>slot &lt; 6</c> bound, which is why the stock is paged rather than scrolled.
    /// The cells are consequently ~2.5x wider and 2x taller than a member's — the room the price
    /// and name lines need, and the reason those lines overlap when drawn into member cells.
    ///
    /// <para>The cells are built at these constants, NOT read from the REQ: elements 7..34 of
    /// REQ_INV are blank templates (ActionId -1, zero size) that the builder fills in.</para>
    ///
    /// <para>One faithful detail this cannot express: the bottom row is one VGA pixel taller than
    /// the top (61 vs 60), so the two rows tile exactly. A uniform cell height loses 6 canonical
    /// pixels at the very bottom edge, which no art depends on.</para>
    /// </remarks>
    public LayoutHint ShopGridArea { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(65f),
        Top = LayoutLength.Px(66f),
        Grid = new LayoutGrid {
            CellWidth = LayoutLength.Px(490f),
            CellHeight = LayoutLength.Px(360f),
            Columns = 3,
            Rows = 2,
        },
    };

    /// <summary>
    /// How far above the price the shop cell's name line sits — one font height.
    /// </summary>
    /// <remarks>
    /// A length, not a constant in the renderer: the cell text stacks upward from the cell's bottom
    /// edge, so this moves with the cell size and with whatever the override author does to it.
    /// <see cref="ItemGridRenderer"/> owns no coordinates and this is one of them.
    /// </remarks>
    public LayoutLength ShopNameLineOffset { get; set; } = LayoutLength.Px(60f);

    /// <summary>How many items a shop shows before it has to page — the builder's <c>slot &lt; 6</c>
    /// bound, and the threshold the next-page control appears above.</summary>
    public int ShopPageSize { get; set; } = 6;

    /// <summary>Member/shop mode: every general (non-equipped) item is nudged this far right,
    /// into the right-hand panel box, clear of the paperdoll's reserved columns. VGA 12 ->
    /// canonical 60 (the member shift, <c>sub_ovr157_0</c> @0x545f3).</summary>
    public LayoutLength MemberShiftX { get; set; } = LayoutLength.Px(60f);

    /// <summary>Loot mode's centering box: the packed cluster of items (unshifted, unlike member
    /// mode) is centered inside this box after packing — the box itself is never drawn. VGA
    /// height 132 -> canonical 792, measured from the grid area's own top (the loot centering
    /// pass, <c>sub_ovr157_0</c> @0x54663).
    ///
    /// <para><b>Only the vertical axis is box-driven, and that is why only a Height is stated.</b>
    /// The original centres both axes inside this box with integer division, but horizontally the
    /// truncation always discards exactly half a unit of its own pixel grid — a quantum this
    /// engine-independent geometry cannot express. On the shipped numbers the truncated box form
    /// is provably identical to "centre the cluster on the frame's centre line" (the algebra is in
    /// <c>ItemGridRenderer.ResolveGridOrigin</c>), so the renderer centres horizontally on the
    /// frame, which is exact and reflows. Re-adding a Width here would state a horizontal rule
    /// nothing applies: honouring it would move the shipped screen 2.5 design px right on every
    /// loot render, and not honouring it is data that lies about what it controls.</para></summary>
    public LayoutHint LootBox { get; set; } = new LayoutHint {
        Height = LayoutLength.Px(792f),
    };

    // ---- black item-panel fills (UI_DrawInventory @0x5687d) -------------------------------
    // The screen's item area is blacked before anything is drawn into it. Which boxes are painted
    // depends on the mode: loot (and the item-inspect view) paint ONE continuous box; a member's
    // own inventory paints the paperdoll and general boxes separately, leaving the background
    // art's divider showing in the gap between them.

    /// <summary>The paperdoll fill in member mode — and, by construction, the equip drop target:
    /// <c>invui_handle_item_drag</c> (INVENTOR.C:609, compares at 0x57307-0x5731e) accepts a drop
    /// strictly inside this same rect, so the visible box <i>is</i> the target and the two cannot
    /// drift apart. VGA (13,11,82,121) -> canonical (65,66,410,726).</summary>
    public LayoutHint PaperdollBox { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(65f),
        Top = LayoutLength.Px(66f),
        Width = LayoutLength.Px(410f),
        Height = LayoutLength.Px(726f),
    };

    /// <summary>The general-items fill in member mode, right of the paperdoll box. VGA
    /// (105,11,202,121) -> canonical (525,66,1010,726).</summary>
    public LayoutHint GeneralItemsBox { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(525f),
        Top = LayoutLength.Px(66f),
        Width = LayoutLength.Px(1010f),
        Height = LayoutLength.Px(726f),
    };

    /// <summary>The single continuous fill used in loot mode and in the item-inspect view — the
    /// paperdoll and general boxes merged, divider and all. VGA (13,11,294,121) -> canonical
    /// (65,66,1470,726). This is also the rect the original re-clears on every frame of the
    /// icon-flight animation (<c>invinspect_animate_item_move</c>).</summary>
    public LayoutHint FullItemsBox { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(65f),
        Top = LayoutLength.Px(66f),
        Width = LayoutLength.Px(1470f),
        Height = LayoutLength.Px(726f),
    };

    /// <summary>Optional override for the top-left of the empty-crossbow silhouette
    /// (<c>INVSHP2.BMX#10</c>), drawn in member mode when the slot is unoccupied — skipped for
    /// casters, who cannot carry one (<c>invui_grid_render</c> INVENTOR.C:402-406 /
    /// <c>UI_DrawInventory</c> @0x56990). The sprite keeps its native size, so only a position is
    /// needed.
    ///
    /// <para><b>Null is the default and the recommended state.</b> Null means "derive from
    /// <see cref="GridArea"/>": the silhouette sits at its own paperdoll cell's top-left (column 0,
    /// row 1), nudged down by <see cref="PaperdollPlaceholderNudgeY"/> — which reproduces the
    /// original's canonical (70,258) exactly, from the same grid the equipped-item cells are placed
    /// against. The point of deriving is that the silhouette then FOLLOWS a resized grid: change
    /// <c>GridArea.Grid.CellHeight</c> and the crossbow cell and its silhouette move together. A
    /// fixed point does not — it would stay put while the cell moved out from under it, and the
    /// armor silhouette would end up inside the crossbow slot.</para>
    ///
    /// <para>A non-null value is honoured verbatim as an explicit override. Setting one is taking
    /// responsibility for keeping the silhouette with its cell across every other change to
    /// <see cref="GridArea"/>.</para></summary>
    public LayoutHint? CrossbowPlaceholder { get; set; }

    /// <summary>Optional override for the top-left of the empty-armor silhouette
    /// (<c>INVSHP2.BMX#11</c>), drawn in member mode whenever the slot is unoccupied. There is no
    /// sword/staff placeholder in the original.
    ///
    /// <para>Null — the default and recommended state — derives it from <see cref="GridArea"/>:
    /// the armor paperdoll cell (column 0, row 2) nudged down by
    /// <see cref="PaperdollPlaceholderNudgeY"/>, which is the original's canonical (70,438). See
    /// <see cref="CrossbowPlaceholder"/> for why deriving is preferred to a fixed point and what an
    /// override signs up for.</para></summary>
    public LayoutHint? ArmorPlaceholder { get; set; }

    /// <summary>
    /// The party-money readout, drawn on every render of this screen in ALL its modes — a member's
    /// own inventory, a loot container, the combat inventory and a shop (<c>UI_DrawInventory</c>
    /// @0x56dd0, INVENTOR.C:530-543). VGA (259,183) -> canonical (1295,1098).
    ///
    /// <para>Authored as a <b>Right</b> inset because the original draws it with
    /// <c>alignment == 2</c>, which <c>invui_draw_text_aligned_shadow</c> implements as
    /// <c>x -= textWidth</c>: 259 is the text's RIGHT edge, not its left, and the text grows
    /// leftward as the party gets richer. 1600 - 1295 = 305 from the frame's right edge.</para>
    ///
    /// <para>That edge is not arbitrary. <c>REQ_INV.DAT</c> entry 6 is an invisible ClickArea with
    /// action id 34 (<c>0x22</c>) spanning canonical (975,1080)-(1295,1170) — the readout's own hit
    /// target, whose right edge is exactly this anchor. Moving one without the other separates the
    /// number from its button.</para>
    /// </summary>
    /// <para>The <see cref="LayoutAnchor.TopRight"/> anchor is load-bearing, not decoration. The
    /// default TopLeft anchor pins <c>left: 0</c>, and an element pinned on BOTH edges is stretched
    /// between them — the readout's box would span the whole frame and only its text alignment
    /// would still look right, so anything that measured the element (an override giving it a
    /// Width, a hit test, a background) would get a screen-wide box. Anchoring top-right leaves the
    /// left edge free, which is what lets the number size to itself and grow leftward.</para>
    public LayoutHint MoneyReadout { get; set; } = new LayoutHint {
        Anchor = LayoutAnchor.TopRight,
        Right = LayoutLength.Px(305f),
        Top = LayoutLength.Px(1098f),
    };

    // ---- item-inspect view (UI_showItem @0x5A778) ------------------------------------------
    // Four text lines and an icon, each at a named point rather than in a grid. All four lines
    // are drawn with align == 1, which invui_draw_text_aligned_shadow implements as
    // "x -= width/2" — so Left is the line's CENTRE, not its left edge.

    /// <summary>Centre of the inspected item's icon, which is drawn at its native size. Also the
    /// endpoint of the fly-in from the item's grid cell. VGA (58,71) -> canonical (290,426).</summary>
    public LayoutHint InspectIcon { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(290f),
        Top = LayoutLength.Px(426f),
    };

    /// <summary>First line of the item's name — used <b>only when the name needs two lines</b>; a
    /// name that fits on one is drawn at <see cref="InspectNameSecondLine"/> instead, so a
    /// one-line name sits lower rather than higher. Whether a name wraps is a content question
    /// answered by the text layer, not by this geometry. VGA (58,15) -> canonical (290,90).</summary>
    public LayoutHint InspectNameFirstLine { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(290f),
        Top = LayoutLength.Px(90f),
    };

    /// <summary>Second line of a wrapped item name — and where an unwrapped name is drawn.
    /// VGA (58,25) -> canonical (290,150).</summary>
    public LayoutHint InspectNameSecondLine { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(290f),
        Top = LayoutLength.Px(150f),
    };

    /// <summary>The item's type line, under the name. VGA (58,35) -> canonical (290,210).</summary>
    public LayoutHint InspectTypeLine { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(290f),
        Top = LayoutLength.Px(210f),
    };

    /// <summary>The status line ("Using ...", condition, ...), below the icon. Its centre sits one
    /// original pixel right of the other three lines — an asymmetry in the original, preserved.
    /// VGA (59,101) -> canonical (295,606).</summary>
    public LayoutHint InspectStatusLine { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(295f),
        Top = LayoutLength.Px(606f),
    };

    // ---- "More Info" stat panel (UI_showItemStats @0x5A1DA) ----------------------------------
    // The stat block is a cursor WALK, not a set of named boxes: a section appends its lines at the
    // cursor and then advances it by its own amount. So only the two ORIGINS are positions; every
    // other value here is a distance added to one, which is why they follow the design-frame scalar
    // rule documented below rather than being LayoutLengths — adding a percentage to a px cursor
    // needs a resolved parent size, i.e. a layout solver.
    //
    // Every advance is named separately even where two happen to share a number today (StaffNudge
    // and LineAdvance are both one original row; ArmorOffsetY and HeaderHeight are both 15). They
    // are independent immediates in the original, and collapsing them here would assert a shared
    // identity the disassembly does not show — and would silently move two things when an override
    // meant to move one.

    /// <summary>Where the stat walk starts for every category except the melee table. VGA (140,30)
    /// -> canonical (700,180) (<c>UI_showItemStats</c> @0x5A1DA).</summary>
    public LayoutHint StatsOrigin { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(700f),
        Top = LayoutLength.Px(180f),
    };

    /// <summary>Where the walk starts for a sword or staff: the two-column Thrust|Swing table needs
    /// more width, so the original shifts the cursor left. VGA (115,30) -> canonical (575,180).
    /// Only the horizontal origin differs from <see cref="StatsOrigin"/> in the shipped data; both
    /// axes are honoured so an override can move the melee table on its own.</summary>
    public LayoutHint StatsWeaponOrigin { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(575f),
        Top = LayoutLength.Px(180f),
    };

    /// <summary>Thrust column offset from the walk's x, in design-frame px. VGA +85 -> 425.</summary>
    public float StatsThrustColumn { get; set; } = 425f;

    /// <summary>Swing column offset from the walk's x, in design-frame px. VGA +150 -> 750.</summary>
    public float StatsSwingColumn { get; set; } = 750f;

    /// <summary>Offset from the walk's x to the value of every SINGLE-column row (ranged damage,
    /// armor mod, enchantments, racial), in design-frame px. VGA +70 -> 350.</summary>
    public float StatsValueColumn { get; set; } = 350f;

    /// <summary>One row of the walk, in design-frame px — the advance between consecutive lines
    /// within a section. VGA +10 -> 60.</summary>
    public float StatsLineAdvance { get; set; } = 60f;

    /// <summary>Extra space inserted before a new section (enchantments, racial, the statistics
    /// line), in design-frame px. VGA +6 -> 36.</summary>
    public float StatsSectionGap { get; set; } = 36f;

    /// <summary>Distance from the melee table's header row down to its first data row, in
    /// design-frame px. VGA +15 -> 90.</summary>
    public float StatsHeaderHeight { get; set; } = 90f;

    /// <summary>How far below the melee header its "________" underline sits, in design-frame px.
    /// VGA +3 -> 18. Small because it underlines the same row rather than starting a new
    /// one.</summary>
    public float StatsUnderlineOffset { get; set; } = 18f;

    /// <summary>A staff's table starts one row lower than a sword's, in design-frame px. VGA +10
    /// -> 60 (<c>UI_showItemStats</c>: the staff branch adds a row before drawing).</summary>
    public float StatsStaffNudge { get; set; } = 60f;

    /// <summary>Distance from the walk's start down to a crossbow/quarrel block's first row, in
    /// design-frame px. VGA +25 -> 150. The ranged block has no header table to clear, so this is
    /// its own advance rather than a header height.</summary>
    public float StatsCrossbowOffsetY { get; set; } = 150f;

    /// <summary>How far right an armor block indents from the walk's x, in design-frame px.
    /// VGA +15 -> 75.</summary>
    public float StatsArmorIndentX { get; set; } = 75f;

    /// <summary>Distance from the walk's start down to the armor block's first row, in design-frame
    /// px. VGA +15 -> 90.</summary>
    public float StatsArmorOffsetY { get; set; } = 90f;

    // ---- design-frame scalars ----------------------------------------------------------------
    // The values below are deliberately plain floats rather than LayoutHint/LayoutLength, because
    // none of them can be expressed in any unit BUT design-frame px:
    //   * a text shadow offset is added to a line's own position, and adding a percentage to a px
    //     inset (or vice versa) needs a resolved parent size, i.e. a layout solver;
    //   * the icon-flight step is a spacing in the panel's own drawing space;
    //   * the empty-slot silhouette nudge is added to a derived cell top, same problem as above;
    //   * the drag threshold is compared against a 2-D pointer distance, which has no axis to take
    //     a percentage of;
    //   * UI Toolkit border widths are px-only floats — there is no percentage form to honour.
    // Typing them as LayoutLength would let an override write "1%" and have the unit silently
    // discarded, which is a worse lie than the missing expressiveness. If one of them ever needs
    // to reflow, the fix is to give it a unit the consumer can actually resolve — not to widen the
    // type and drop the unit on the floor.

    /// <summary>How far right the drop shadow under game-font text is offset, in design-frame px.
    /// One original pixel (<c>invui_draw_text_aligned_shadow</c> draws the same string twice, the
    /// shadow at +1,+1 in pen 0) -> canonical 5.</summary>
    public float TextShadowOffsetX { get; set; } = 5f;

    /// <summary>How far down the drop shadow under game-font text is offset, in design-frame px.
    /// One original pixel -> canonical 6. Different from <see cref="TextShadowOffsetX"/> because
    /// the original's pixels were not square.</summary>
    public float TextShadowOffsetY { get; set; } = 6f;

    /// <summary>Horizontal granularity of the inspect icon's flight, in design-frame px
    /// (<c>invinspect_animate_item_move</c> @0x5A0CB). The animation is a position <i>lattice</i>,
    /// not a smooth interpolation: each frame advances a twelfth of the remaining distance plus
    /// one step, and the "+1 step" is what puts a floor on the speed and guarantees the icon
    /// arrives. The original's step was one of its own pixels, so a step of canonical 5 reproduces
    /// its pacing exactly; a smaller step makes the tail of the flight visibly slower, not
    /// smoother. Set this to 1 for a genuinely continuous flight.</summary>
    public float IconFlightStepX { get; set; } = 5f;

    /// <summary>Vertical granularity of the inspect icon's flight, in design-frame px. See
    /// <see cref="IconFlightStepX"/>; canonical 6 = one original pixel down.</summary>
    public float IconFlightStepY { get; set; } = 6f;

    /// <summary>How far below its paperdoll cell's top edge an empty-slot silhouette is blitted,
    /// in design-frame px. One original pixel down -> canonical 6.
    ///
    /// <para>This exists because the original does not draw the silhouettes flush with their
    /// cells: the crossbow cell starts at VGA y=42 and its silhouette at y=43, the armor cell at
    /// y=72 and its silhouette at y=73 (<c>UI_DrawInventory</c> @0x56990). One pixel of inset, the
    /// same for both — so it is one named number rather than two coordinates, and the silhouettes
    /// stay attached to their cells when the grid is resized.</para>
    ///
    /// <para>A design-frame px scalar rather than a <see cref="LayoutLength"/> for the reason
    /// stated above: it is ADDED to the cell's own top inset, and adding a percentage to a px
    /// inset needs a resolved parent size.</para></summary>
    public float PaperdollPlaceholderNudgeY { get; set; } = 6f;

    // ---- interaction ------------------------------------------------------------------------

    /// <summary>How far a press must travel, in design-frame px, before it counts as a drag rather
    /// than a click (<c>sub_ovr158_1013</c> @0x57184). Roughly 4 original px -> canonical 20.
    /// Compared against the pointer's travel distance, so it is a single scalar rather than a
    /// per-axis pair. Non-positive is degenerate — every pointer movement would become a drag and
    /// nothing could be selected — and the view falls back to this default rather than honour
    /// it.</summary>
    public float DragThreshold { get; set; } = 20f;

    /// <summary>Left/right border width, in design-frame px, of the container window's drag-time
    /// highlight (<c>sub_ovr158_3D0</c> @0x56420 draws a one-pixel frame). One original px ->
    /// canonical 5.</summary>
    public float ContainerBorderWidthX { get; set; } = 5f;

    /// <summary>Top/bottom border width, in design-frame px, of the container window's drag-time
    /// highlight. One original px -> canonical 6.</summary>
    public float ContainerBorderWidthY { get; set; } = 6f;
}
