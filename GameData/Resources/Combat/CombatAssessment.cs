namespace GameData.Resources.Combat;

using System;
using System.Collections.Generic;

/// <summary>
/// What Inspect shows you about an enemy — <c>combatenc_anim_actor_stat_rolls</c>
/// (canassa CBENC.C:307) and its line drawer at CBENC.C:269.
/// </summary>
/// <remarks>
/// <b>THIS IS WHAT THE ASSESSMENT SKILL IS FOR, and it was the one thing our port had no consumer
/// for.</b> Each row is offered separately and drawn only if <c>RND(100) &lt;= inspectorAssessment</c>
/// — a poor assessor learns one or two of the enemy's numbers, a good one learns most of them. The
/// roll is against the <b>inspector's</b> skill (<c>stat_actor_get(g_current_actor, 8, 0)</c>), not
/// against anything the target has.
///
/// <para><b>You always learn something.</b> The whole set is re-offered until at least one row
/// survives its roll, so a character with Assessment 1 is slow rather than blind. That retry is the
/// outer <c>while (state == 0)</c> loop, and missing it would leave a low-skill inspection showing
/// an empty screen and spending the turn.</para>
///
/// <para><b>It is NOT the HUD stats panel</b>, which is
/// <see cref="ActorStatsPanel"/>: different rows, different place, and a dialog record either side.
/// <c>combat_arena_switch_active_actor</c> — the routine the Inspect arm calls — never assigns
/// <c>g_current_actor</c>; it unloads the spell subsystem for heap room, calls this, and reloads.
/// See <see cref="InspectAction"/>.</para>
/// </remarks>
public static class CombatAssessment {
    /// <summary>Dialog record played before the numbers appear.</summary>
    /// <remarks>
    /// Set up with <c>nEvtArgActor0 = inspector's character slot - 1</c> and
    /// <c>nEvtArgAux1 = the target's creature type</c>, so the line names both.
    /// </remarks>
    public const int OpeningDialog = 0x84;

    /// <summary>Dialog record played once the numbers are up.</summary>
    public const int ClosingDialog = 0x85;

    /// <summary>The attribute the reveal rolls against — the INSPECTOR's.</summary>
    public const ActorAttribute RollAttribute = ActorAttribute.Assessment;

    /// <summary>One offered row.</summary>
    public readonly struct Row {
        public Row(ActorAttribute attribute, string label, bool percent) {
            Attribute = attribute;
            Label = label;
            Percent = percent;
        }

        public ActorAttribute Attribute { get; }

        public string Label { get; }

        /// <summary>Whether a "%" is drawn after the number — true for the skills, false for the
        /// pools and the two physical numbers.</summary>
        public bool Percent { get; }
    }

    /// <summary>Left edge of the first column.</summary>
    public const int FirstColumnX = 0x46;

    /// <summary>Top of the first row.</summary>
    public const int FirstRowY = 0x44;

    /// <summary>Distance from a label to its number.</summary>
    public const int ValueOffsetX = 50;

    /// <summary>Distance from a label to the "%" that follows a percentage row.</summary>
    public const int PercentOffsetX = 61;

    /// <summary>Rows advance by this much.</summary>
    public const int RowStep = 10;

    /// <summary>Once a row would be drawn past this y the column wraps.</summary>
    public const int LastRowY = 0x5a;

    /// <summary>A wrapped column starts this far right of the previous one.</summary>
    public const int ColumnStep = 100;

    /// <summary>
    /// <b>CD-only:</b> nothing is drawn once the column origin reaches this x.
    /// </summary>
    /// <remarks>
    /// Three rows fit a column (68, 78, 88 — 98 exceeds <see cref="LastRowY"/>) and the third column
    /// would start at 0x10e, so the 1.02 CD build shows <b>at most six</b> of the eight offered
    /// rows however good the inspector is. The 1.00 floppy has no such guard. We target the CD
    /// build, so the cap applies.
    /// </remarks>
    public const int ColumnLimitX = 0x10e;

    /// <summary>
    /// The rows offered, in order.
    /// </summary>
    /// <param name="targetCanShoot">
    /// <c>combatenc_show_missile_stat_row(target)</c> — the same predicate that offers the Shoot
    /// button, asked of the <b>target</b>. See <see cref="CombatCapability.CanShoot"/>.
    /// </param>
    /// <param name="targetCanCast">
    /// <c>combatenc_actor_can_cast_spells(target, 0)</c> — note the 0, which skips the adjacency
    /// rule the menu's own test applies.
    /// </param>
    /// <remarks>
    /// <b>Two of the eight are conditional on what the TARGET can do</b>, so a creature that cannot
    /// shoot never offers a Missile row even to a perfect assessor — the row is absent, not hidden
    /// behind a failed roll. Both spellings are the original's ("Missle:" is misspelt in the
    /// shipped binary).
    /// </remarks>
    public static IReadOnlyList<Row> RowsFor(bool targetCanShoot, bool targetCanCast) {
        var rows = new List<Row> {
            new Row(ActorAttribute.Health, "Health:", false),
            new Row(ActorAttribute.Stamina, "Stamina:", false),
            new Row(ActorAttribute.Speed, "Speed:", false),
            new Row(ActorAttribute.Strength, "Strength:", false),
        };
        if (targetCanShoot) {
            rows.Add(new Row(ActorAttribute.AccuracyCrossbow, "Missle:", true));
        }
        rows.Add(new Row(ActorAttribute.AccuracyMelee, "Melee:", true));
        if (targetCanCast) {
            rows.Add(new Row(ActorAttribute.AccuracyCasting, "Cast:", true));
        }
        rows.Add(new Row(ActorAttribute.Defense, "Defense:", true));
        return rows;
    }

    /// <summary>Whether one offered row survives its roll.</summary>
    /// <param name="roll">A roll in <c>[0, 100)</c>.</param>
    /// <param name="inspectorAssessment">The INSPECTOR's Assessment.</param>
    /// <remarks><b>Strictly greater fails</b>: <c>if (roll &gt; stat) return;</c>, so a roll equal
    /// to the skill reveals the row.</remarks>
    public static bool Reveals(int roll, int inspectorAssessment) => roll <= inspectorAssessment;

    /// <summary>Where a revealed row is drawn, given how many were revealed before it.</summary>
    /// <returns>Null once the columns are full — the CD build stops drawing.</returns>
    public static (int X, int Y)? PositionOf(int revealedSoFar) {
        int rowsPerColumn = 0;
        for (int y = FirstRowY; y <= LastRowY; y += RowStep) {
            rowsPerColumn++;
        }
        int column = revealedSoFar / rowsPerColumn;
        int x = FirstColumnX + column * ColumnStep;
        if (x >= ColumnLimitX) {
            return null;
        }
        return (x, FirstRowY + revealedSoFar % rowsPerColumn * RowStep);
    }

    /// <summary>
    /// Roll the whole set, re-offering it until something is revealed.
    /// </summary>
    /// <param name="rows">From <see cref="RowsFor"/>.</param>
    /// <param name="inspectorAssessment">The inspector's Assessment.</param>
    /// <param name="valueOf">The target's value for an attribute.</param>
    /// <param name="roll">Returns a value in <c>[0, n)</c>.</param>
    /// <returns>The revealed rows with their positions, in draw order. Never empty when
    /// <paramref name="rows"/> is non-empty.</returns>
    /// <remarks>
    /// <b>The retry re-rolls every row, not just the failed ones</b>, and the pass that finally
    /// reveals something draws every row that passed on THAT pass — so the result is a random
    /// subset of one successful sweep, not an accumulation across sweeps.
    /// </remarks>
    public static IReadOnlyList<(Row Row, int Value, int X, int Y)> Reveal(
        IReadOnlyList<Row> rows, int inspectorAssessment, Func<ActorAttribute, int> valueOf,
        Func<int, int> roll) {
        var revealed = new List<(Row, int, int, int)>();
        if (rows == null || rows.Count == 0 || valueOf == null || roll == null) {
            return revealed;
        }

        // Bounded so an Assessment of 0 cannot spin forever: the original relies on RND(100)
        // eventually rolling 0, which it does, but a caller's stub might not.
        for (var sweep = 0; sweep < MaxSweeps && revealed.Count == 0; sweep++) {
            foreach (Row row in rows) {
                if (!Reveals(roll(100), inspectorAssessment)) {
                    continue;
                }
                (int X, int Y)? at = PositionOf(revealed.Count);
                if (at == null) {
                    break;   // the CD build's column cap
                }
                revealed.Add((row, valueOf(row.Attribute), at.Value.X, at.Value.Y));
            }
        }
        return revealed;
    }

    /// <summary>How many times <see cref="Reveal"/> will re-offer the set before giving up.</summary>
    /// <remarks>
    /// The original loops unbounded. This is a guard against a caller's deterministic roll stub,
    /// not a rule: with a real d100 and any Assessment above zero the first sweep almost always
    /// reveals something.
    /// </remarks>
    public const int MaxSweeps = 1000;
}
