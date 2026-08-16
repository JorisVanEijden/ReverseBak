namespace GameData.Resources.Config;

using GameData;
using GameData.Resources.Character;
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
