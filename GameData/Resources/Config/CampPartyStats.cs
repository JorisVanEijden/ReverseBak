namespace GameData.Resources.Config;

using GameData;
using GameData.Resources.Character;
using GameData.Resources.Text;
using System.Collections.Generic;

/// <summary>
/// The party table on the camp screen — a row per active member with their health-and-stamina and
/// their rations. Faithful port of <c>UI_show_actor_healthStatus</c> @0x70d2d (ovr182,
/// canassa <c>encamp_draw_party_stats</c>).
/// </summary>
/// <remarks>
/// This is what fills the camp panel. Positions are canonical 1600x1200 (VGA x5 across, x6 down).
/// </remarks>
public static class CampPartyStats {
    /// <summary>Baseline of the two column headings — VGA y=21.</summary>
    public const int HeadingY = 126;

    /// <summary>
    /// Both headings are centred on their column, which is the column's x plus this — VGA 134.
    /// </summary>
    /// <remarks>
    /// The original measures each heading and subtracts half its width, so the two are centred
    /// rather than left-aligned on the same offsets the values use. Left-aligning them puts both
    /// headings visibly left of the numbers they label.
    /// </remarks>
    public const int HeadingCentreOffsetX = 670;

    /// <summary>Left edge of the member's name — VGA x=139.</summary>
    public const int NameX = 695;

    /// <summary>
    /// The two value columns' base x, in canonical space — VGA 84 and 144.
    /// </summary>
    /// <remarks>
    /// Read from the original's own column table rather than measured off a screenshot. A heading
    /// centres on <c>base + <see cref="HeadingCentreOffsetX"/></c>, i.e. VGA 218 and 278, which is
    /// where they sit in a capture of the original.
    /// </remarks>
    public static readonly IReadOnlyList<int> ColumnX = new[] { 84 * 5, 144 * 5 };

    /// <summary>The centre a column's heading is laid out around.</summary>
    public static int HeadingCentreX(int column) => ColumnX[column] + HeadingCentreOffsetX;

    /// <summary>Columns in the table: health-and-stamina, then rations.</summary>
    public const int ColumnCount = 2;

    /// <summary>
    /// The baseline of a member's row, by active-roster slot.
    /// </summary>
    /// <remarks>VGA <c>slot * 16 + 37</c>.</remarks>
    public static int RowY(int slot) => ((slot * 16) + 37) * 6;

    // ---- the name's ink -----------------------------------------------------------------------

    /// <summary>Text colour for a member with nothing wrong with them.</summary>
    public const int HealthyTextColour = 0;

    /// <summary>Text colour for a member carrying any affliction.</summary>
    public const int AfflictedTextColour = 107;

    /// <summary>
    /// Whether a member's name is drawn as afflicted.
    /// </summary>
    /// <remarks>
    /// <b>Every condition EXCEPT Healing.</b> The original tests six of the seven slots by name and
    /// simply never looks at Healing — the same rule the temple charges by and the character sheet
    /// draws by, because Healing is the beneficial entry in the vector.
    ///
    /// <para><b>This is NOT <see cref="ActorConditions.None"/> inverted.</b> That property asks
    /// whether the vector is empty, and a regenerating character's is not — so
    /// <c>!None</c> would paint someone who is merely healing as sick. The two look
    /// interchangeable and are not.</para>
    /// </remarks>
    public static bool IsAfflicted(ActorConditions conditions) {
        if (conditions == null) {
            return false;
        }

        for (var slot = 0; slot < ActorConditions.Count; slot++) {
            var condition = (ActorCondition)slot;
            if (condition != ActorCondition.Healing && conditions.Has(condition)) {
                return true;
            }
        }

        return false;
    }

    /// <summary>The colour a member's name is drawn in.</summary>
    public static int NameColour(ActorConditions conditions) =>
        IsAfflicted(conditions) ? AfflictedTextColour : HealthyTextColour;

    // ---- the value columns --------------------------------------------------------------------

    /// <summary>Catalog key for the first column's heading.</summary>
    public const string HealthStaminaLabelKey = "base:uistring:encamp.health_stamina_label";

    /// <summary>Catalog key for the second column's heading.</summary>
    public const string RationsLabelKey = "base:uistring:encamp.rations_label";

    /// <summary>
    /// Catalog key for the separator between current and maximum.
    /// </summary>
    /// <remarks>
    /// <b>This is not <c>attribute.current_of_max_separator</c>.</b> That one is a different call
    /// site and is bare <c>"of"</c>; this one is <c>" of "</c> and its spaces ARE the layout,
    /// because the original concatenates rather than formatting. Reaching for the other key renders
    /// "100of100".
    /// </remarks>
    public const string SeparatorKey = "base:uistring:encamp.current_of_max_separator";

    /// <summary>A member's health-and-stamina reading, e.g. "100 of 100".</summary>
    public static string HealthStaminaText(int current, int maximum) =>
        current.ToString(System.Globalization.CultureInfo.InvariantCulture)
        + Text.UiStrings.Get(SeparatorKey)
        + maximum.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// Every value is centred on its column, exactly as the headings are.
    /// </summary>
    /// <remarks>
    /// Confirmed from the code rather than measured off a capture: the value draw repeats the
    /// heading's <c>columnX + 134 - width/2</c> verbatim.
    /// </remarks>
    public static int ValueCentreX(int column) => HeadingCentreX(column);

    // ---- the wounded highlight -------------------------------------------------------------------

    /// <summary>Below this percentage of maximum, the current figure is highlighted.</summary>
    /// <remarks>
    /// <b>The same 80% the rest loop stops at.</b> Camping runs until every member is above it, and
    /// the table highlights anyone below it — one threshold wearing two hats, so a rest ends exactly
    /// when the last highlight clears.
    /// </remarks>
    public const int WoundedPercent = 80;

    /// <summary>Colour the current figure is redrawn in when the member is below the threshold.</summary>
    /// <remarks>The same ink an afflicted name uses; see <see cref="AfflictedTextColour"/>.</remarks>
    public const int WoundedTextColour = AfflictedTextColour;

    /// <summary>Whether a member's health-and-stamina is low enough to highlight.</summary>
    /// <remarks>
    /// The original computes <c>max * 80 / 100 &gt; current</c> — integer arithmetic, and the
    /// comparison is strict, so a member sitting exactly on the threshold is NOT highlighted.
    /// </remarks>
    public static bool IsWounded(int current, int maximum) => maximum * WoundedPercent / 100 > current;

    /// <summary>
    /// The highlight is drawn by <b>overprinting the current figure alone</b>, at the same position
    /// the whole "N of M" string starts.
    /// </summary>
    /// <remarks>
    /// That works because the current figure is the leading part of the string, so redrawing just
    /// it recolours the number and leaves " of M" in the plain ink. Recolouring the whole line would
    /// tint the maximum too, which the original never does.
    /// </remarks>
    public static bool HighlightOverprintsTheCurrentValueOnly => true;

    // ---- the rations column ---------------------------------------------------------------------

    /// <summary>Edible object ids the rations count adds up.</summary>
    /// <remarks>
    /// <b>All three kinds count, spoiled and poisoned included.</b> The original sums Rations,
    /// Rations (Poisoned) and Rations (Spoiled) into one figure, so the column answers "how many
    /// meals are in the pack", not "how many are safe to eat". Counting only the good ones would
    /// show a party with a bag of spoiled food as having nothing.
    /// </remarks>
    public static readonly IReadOnlyList<int> RationObjectIds = new[] { 72, 73, 74 };

    /// <summary>The rations figure for a member, given a count of each object id they carry.</summary>
    public static int RationsFor(System.Func<int, int> countOf) {
        if (countOf == null) {
            return 0;
        }

        var total = 0;
        foreach (int objectId in RationObjectIds) {
            total += countOf(objectId);
        }

        return total;
    }
}
