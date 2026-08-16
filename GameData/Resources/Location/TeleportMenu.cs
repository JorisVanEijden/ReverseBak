namespace GameData.Resources.Location;

/// <summary>
/// The rift-map screen a temple shows when the party asks to be sent somewhere — which of the
/// twelve temples it offers, what it says when it offers none, and where the panel puts its text.
/// Faithful port of <c>UI_teleportation</c> @0x4ee7e and <c>drawTeleportMenu</c> @0x4ecff (ovr150).
///
/// <para>The fare itself is <see cref="TeleportCost"/>; the destinations are
/// <see cref="TeleportDestinationSet"/>. This class is the screen's own rules, and only those.</para>
/// </summary>
public static class TeleportMenu {
    /// <summary>The twelve temples on the map, numbered 1-12 as the original numbers them.</summary>
    public const int TempleCount = 12;

    /// <summary>
    /// <c>REQ_TELE.DAT</c>'s action id for the first temple pin; the rest follow in order, so pin
    /// <c>n</c> is <c>PinActionIdBase + n - 1</c>.
    /// </summary>
    /// <remarks>
    /// The original tests <c>actionId &gt; 0x80</c> to tell a pin from the Cancel button rather
    /// than range-checking it, which is why the pins start one past 128 rather than at it.
    /// </remarks>
    public const int PinActionIdBase = 129;

    /// <summary>The Cancel button's action id — the shared REQ convention, and what Escape maps to.</summary>
    public const int CancelActionId = 1;

    /// <summary>The temple a REQ action id selects, or 0 when the id is not a pin.</summary>
    public static int TempleForAction(int actionId) =>
        actionId >= PinActionIdBase && actionId < PinActionIdBase + TempleCount
            ? actionId - PinActionIdBase + 1
            : 0;

    /// <summary>The REQ action id of a temple's pin.</summary>
    public static int ActionIdForTemple(int temple) => PinActionIdBase + temple - 1;

    /// <summary>
    /// The <c>TELEPORT.DAT</c> row a temple lands the party on.
    /// </summary>
    /// <remarks>
    /// Temples are 1-based on this screen and 0-based in the file, and the original converts at the
    /// call rather than storing either form — so rows 0-11 are the twelve pins in map order, and
    /// everything above them belongs to dialog teleports instead (see
    /// <see cref="TeleportDestinationSet"/>).
    /// </remarks>
    public static int DestinationIdForTemple(int temple) => temple - 1;

    // ---- availability ---------------------------------------------------------------------

    /// <summary>
    /// Base of the per-temple "the party has been here" flags: temple <c>n</c> is
    /// <c>TempleVisitedFlagBase + n</c>.
    /// </summary>
    /// <remarks>
    /// Written on arrival by the GDS scene itself (<c>GdsScene.EntryFlag</c>), read here. That split
    /// is the whole mechanic: <b>you can only teleport to a temple you have walked into at least
    /// once</b>, so the map fills in as the party travels.
    /// </remarks>
    public const int TempleVisitedFlagBase = 6480;

    /// <summary>The global flag that records whether a temple has been visited.</summary>
    public static int VisitedFlagFor(int temple) => TempleVisitedFlagBase + temple;

    /// <summary>
    /// Whether a temple is offered as a destination.
    /// </summary>
    /// <param name="temple">The candidate destination, 1-12.</param>
    /// <param name="currentTemple">The temple the party is standing in.</param>
    /// <param name="visited">Whether <paramref name="temple"/>'s visited flag is set.</param>
    /// <remarks>
    /// A temple never offers to send the party to itself, and an unvisited one is not offered at
    /// all. <b>Both cases are removed rather than greyed out</b> — the original clears the pin's
    /// marker entirely, so an unreached temple leaves no dot on the map to wonder about.
    /// </remarks>
    public static bool IsOffered(int temple, int currentTemple, bool visited) =>
        temple != currentTemple && visited;

    /// <summary>
    /// Whether the screen is worth opening at all — false when the party knows no temple but this
    /// one, which the original answers with <see cref="NoOtherTemplesDialog"/> instead of an empty
    /// map.
    /// </summary>
    /// <param name="currentTemple">The temple the party is standing in.</param>
    /// <param name="isVisited">Whether a given temple number's visited flag is set.</param>
    public static bool AnyDestinationOffered(int currentTemple, System.Func<int, bool> isVisited) {
        for (int temple = 1; temple <= TempleCount; temple++) {
            if (IsOffered(temple, currentTemple, isVisited(temple))) {
                return true;
            }
        }

        return false;
    }

    // ---- the Malac's Cross exception ------------------------------------------------------

    /// <summary>The Chapel of Ishap in Malac's Cross — pin 12, and the one temple with a story gate.</summary>
    public const int ChapelOfIshap = 12;

    /// <summary>The chapter during which the chapel's rift is disturbed.</summary>
    public const int ChapelClosedChapter = 6;

    /// <summary>The flag that reopens the chapel; while it is clear the chapel neither sends nor receives.</summary>
    public const int ChapelReopenedFlag = 7892;

    /// <summary>
    /// Whether the chapel's rift is shut.
    /// </summary>
    /// <remarks>
    /// <b>The same condition produces two different refusals</b>, and which one the player hears
    /// depends on which end they are at: standing in the chapel gets
    /// <see cref="ChapelRefusesServiceDialog"/> (the acolytes will do nothing but vital healings),
    /// while picking it as a destination from elsewhere gets <see cref="ChapelUnreachableDialog"/>
    /// (a disturbance the sending temple cannot do anything about). Collapsing them to one message
    /// would lose that the blockage is local to Malac's Cross, which is the point of the scene.
    /// </remarks>
    public static bool ChapelIsClosed(int chapter, bool reopened) =>
        chapter == ChapelClosedChapter && !reopened;

    // ---- dialogs --------------------------------------------------------------------------

    /// <summary>Shown on arrival at the screen — the mandala on the temple wall.</summary>
    public const int IntroDialog = 1300061;

    /// <summary>Shown instead of the map when the party has seen no other temple's symbol.</summary>
    public const int NoOtherTemplesDialog = 1300063;

    /// <summary>Right-click (or Shift-click) help. See <see cref="HelpTopicGlobal"/>.</summary>
    public const int HelpDialog = 1300067;

    /// <summary>The arrival narration, shown after the fare is deducted.</summary>
    public const int ArrivalDialog = 1300064;

    /// <summary>Shown when the party leaves without travelling.</summary>
    public const int DeclinedDialog = 1300065;

    /// <summary>Shown when the chosen fare turns out to be more than the purse holds.</summary>
    public const int CannotAffordDialog = 1300062;

    /// <summary>Standing in the chapel while its rift is shut: no service but vital healings.</summary>
    public const int ChapelRefusesServiceDialog = 300030;

    /// <summary>Choosing the chapel as a destination while its rift is shut.</summary>
    public const int ChapelUnreachableDialog = 300029;

    /// <summary>
    /// The global the help dialog reads to pick its topic: 0 for the Cancel button, 1 for a pin.
    /// </summary>
    public const int HelpTopicGlobal = 30000;

    /// <summary>Which help topic a right-click on the given action id asks for.</summary>
    public static int HelpTopicFor(int actionId) => actionId == CancelActionId ? 0 : 1;

    // ---- artwork --------------------------------------------------------------------------

    /// <summary>The screen's backdrop; the map picture is part of it.</summary>
    public const string Backdrop = "C42.SCX";

    /// <summary>Every piece of art the screen draws itself, temple portraits included.</summary>
    public const string IconSet = "TELEPORT.BMX";

    /// <summary>The palette the screen runs under.</summary>
    public const string Palette = "TELEPORT.PAL";

    /// <summary>The layout whose twelve pins are the map's hit-boxes.</summary>
    public const string HotspotLayout = "REQ_TELE.DAT";

    /// <summary>A temple's portrait in <see cref="IconSet"/> — the picture beside its name.</summary>
    public static int PortraitIcon(int temple) => temple - 1;

    /// <summary>The "Teleport" heading.</summary>
    public const int TitleIcon = 12;

    /// <summary>The marker on the temple the party is standing in.</summary>
    public const int SourcePinIcon = 13;

    /// <summary>The marker on the destination under the cursor.</summary>
    public const int DestinationPinIcon = 14;

    /// <summary>The marker on every other offered destination.</summary>
    public const int OfferedPinIcon = 15;

    /// <summary>First frame of the spark that flies between the pins.</summary>
    public const int FirstSparkIcon = 16;

    /// <summary>How many frames the spark cycles through.</summary>
    public const int SparkFrameCount = 5;

    /// <summary>The spark frame for a step of the flight.</summary>
    public static int SparkIcon(int step) => FirstSparkIcon + (step % SparkFrameCount);

    /// <summary>
    /// The marker a pin draws, or -1 to draw nothing.
    /// </summary>
    /// <param name="temple">The pin being drawn.</param>
    /// <param name="currentTemple">The temple the party is standing in.</param>
    /// <param name="hoveredTemple">The destination under the cursor, or 0 for none.</param>
    /// <param name="offered">Whether this pin is an offered destination.</param>
    /// <remarks>
    /// <b>The source pin outranks everything</b>, including its own exclusion from the offered set —
    /// the party's own temple is not a destination but must still show on the map, or the map has no
    /// "you are here". The hover marker comes next, then the plain offered marker.
    /// </remarks>
    public static int PinIcon(int temple, int currentTemple, int hoveredTemple, bool offered) {
        if (temple == currentTemple) {
            return SourcePinIcon;
        }

        if (temple == hoveredTemple) {
            return DestinationPinIcon;
        }

        return offered ? OfferedPinIcon : -1;
    }

    // ---- the flight -----------------------------------------------------------------------

    /// <summary>
    /// How far the spark strays from the straight line between two pins, at a given step.
    /// </summary>
    /// <param name="step">Steps taken so far, 0 to <paramref name="length"/>.</param>
    /// <param name="length">Steps in the whole flight — the longer of the two pin separations.</param>
    /// <returns>The displacement perpendicular to the flight's long axis.</returns>
    /// <remarks>
    /// Half a sine period across the whole flight, so the spark bows out once and lands flat rather
    /// than snaking. <b>The bow's height is a sixth of the flight's length</b>, which makes a long
    /// journey arc grandly and a short hop travel nearly straight — the arc reads as distance.
    ///
    /// <para>The original reads its own quarter-wave table (<c>get_sine</c> @0x26528, 14-bit fixed
    /// point) rather than computing this; the table is an implementation detail of a machine without
    /// an FPU, and the curve is the behaviour.</para>
    /// </remarks>
    public static int FlightArcOffset(int step, int length) {
        if (length <= 0) {
            return 0;
        }

        double phase = System.Math.PI * step / length;
        return (int)(System.Math.Sin(phase) * length / 6);
    }

    /// <summary>The spark's offset from a pin's top-left corner, in VGA pixels.</summary>
    public const int SparkOffsetX = 3;

    /// <summary>The spark's offset from a pin's top-left corner, in VGA pixels.</summary>
    public const int SparkOffsetY = 1;

    // ---- panel layout ---------------------------------------------------------------------
    //
    // Canonical 1600x1200 (VGA x5 across, x6 down). The screen's left column is a fixed panel:
    // heading, "From:" over the source, "To:" over the destination, and the fare at the foot.
    // Everything but the fare is centred on the column.
    //
    // Not modelled: the four nested bevel rectangles the original draws around the map at
    // 0x4ef41-0x4ef8c. They are chrome over C42.SCX and carry no information.

    /// <summary>Centre of the left-hand panel, which every line but the fare is centred on.</summary>
    public const int PanelCentreX = 300;

    /// <summary>
    /// Pen the captions are drawn in — "From:", "To:" and "Cost:", flat with no shadow behind them.
    /// </summary>
    /// <remarks>
    /// <b>Captions and values are drawn differently on purpose.</b> A caption is a flat pen-0 word
    /// with the shadow argument passed as -2 (the original's "none"); a value is pen 10 over a pen-1
    /// drop shadow, which lifts it off the map behind. That contrast is the only thing separating
    /// the label from the answer in a panel with no rules or boxes in it, so drawing both the same
    /// makes "From: Temple of Sung" read as one run-on line.
    /// </remarks>
    public const int CaptionPen = 0;

    /// <summary>Pen the values are drawn in — the temple names and the fare.</summary>
    public const int ValuePen = 10;

    /// <summary>Pen of the drop shadow behind a value. Captions have none.</summary>
    public const int ValueShadowPen = 1;

    /// <summary>Top of the "Teleport" heading.</summary>
    public const int TitleY = 60;

    /// <summary>Baseline of the "From:" caption.</summary>
    public const int FromCaptionY = 210;

    /// <summary>Baseline of the source temple's name.</summary>
    public const int SourceNameY = 264;

    /// <summary>Top of the source temple's portrait.</summary>
    public const int SourcePortraitY = 330;

    /// <summary>Baseline of the "To:" caption.</summary>
    public const int ToCaptionY = 642;

    /// <summary>Baseline of the destination temple's name.</summary>
    public const int DestinationNameY = 696;

    /// <summary>Top of the destination temple's portrait.</summary>
    public const int DestinationPortraitY = 762;

    /// <summary>Centre of the "Cost:" caption, which sits left of the amount rather than above it.</summary>
    public const int CostCaptionCentreX = 120;

    /// <summary>Left edge of the fare, which is left-aligned where every other line is centred.</summary>
    public const int CostAmountX = 190;

    /// <summary>Baseline shared by the "Cost:" caption and the fare.</summary>
    public const int CostY = 1080;
}
