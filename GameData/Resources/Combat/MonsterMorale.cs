namespace GameData.Resources.Combat;

/// <summary>
/// Whether a creature routs — <c>combatenc_ai_morale_flee_check</c> (<c>SRC/COMBAT/ENC/CBENC.C</c>).
///
/// <para>This runs <b>before</b> everything else on a monster's turn: a fleeing actor leaves the
/// field and never reaches the class routine or the capability cascade
/// (<see cref="CombatAi.ChooseAction"/>).</para>
/// </summary>
public static class MonsterMorale {
    /// <summary>Morale value meaning "never routs", checked before anything else.</summary>
    public const int NeverFleesMorale = 0xff;

    /// <summary>Highest index the threshold table is read at.</summary>
    public const int MaxIndex = 9;

    /// <summary>The morale term's pivot: a creature at 8 gets no adjustment at all.</summary>
    public const int MoralePivot = 8;

    /// <summary>
    /// <b>The stat's polarity is the opposite of what its names suggest.</b> A HIGHER value makes a
    /// creature MORE likely to rout, and 0 means it never does.
    /// </summary>
    /// <remarks>
    /// The index term is <c>8 - value</c>, and a larger index reaches the calm end of the threshold
    /// table — so 8 lands on the 85%-rout entry and 0 lands past the far end. canassa calls the
    /// field <c>morale</c> and our extractor calls it
    /// <see cref="Monster.MonsterStats.FleeThreshold"/>; read either as English and the sign comes
    /// out backwards, giving brave monsters that run and cowardly ones that never do.
    ///
    /// <para>Corroborated by the shipped data: MONST.DAT values run 0..8, and MONST19 and MONST28
    /// ship {0, 0} — creatures that can never rout, which is what the morale-0 guard below is for.
    /// </para>
    /// </remarks>
    public const bool HigherValueMeansMoreLikelyToRout = true;

    /// <summary>
    /// Where this creature sits in the flee-threshold table: <c>staminaPercent / 10 - 1</c>, shifted
    /// by <c>8 - morale</c>, capped at <see cref="MaxIndex"/>.
    /// </summary>
    /// <remarks>
    /// <b>The original caps the top and not the bottom.</b> Stamina at 0% gives -1, and a creature
    /// of morale 8 or better adds nothing or less — so the index can go negative and the original
    /// reads off the front of a ten-entry table. We clamp instead, which is the same answer
    /// everywhere the original is in bounds and a defined one where it is not: index 0 is the
    /// most-likely-to-rout end, which is where a creature on no stamina belongs anyway.
    /// </remarks>
    public static int IndexFor(int staminaPercent, int morale) {
        int index = (staminaPercent / 10) - 1 + (MoralePivot - (sbyte)morale);
        if (index > MaxIndex) {
            return MaxIndex;
        }
        return index < 0 ? 0 : index;
    }

    /// <summary>
    /// Whether this creature routs this turn.
    /// </summary>
    /// <param name="staminaPercent">Its stamina as a percentage — the original reads stat 1.</param>
    /// <param name="morale">Its morale. Both <see cref="NeverFleesMorale"/> and 0 never rout.</param>
    /// <param name="rollPercent">A <c>RND(100)</c> roll.</param>
    /// <param name="thresholds">
    /// <c>g_ai_flee_threshold_table</c> — <c>AiFleeThresholds</c> in
    /// <see cref="CombatAffinityTables"/>, extracted as {85, 55, 45, 35, 25, 20, 10, 5, 5, 0}.
    /// </param>
    /// <param name="isUnderground">
    /// <b>Nothing routs underground.</b> The check returns immediately in game mode 2, so a dungeon
    /// fight is always to the finish however badly a creature is losing. Easy to miss, and its
    /// absence would make dungeon encounters feel completely different.
    /// </param>
    /// <remarks>
    /// <b>The two never-flee morale values are tested at different points, and that matters.</b>
    /// <see cref="NeverFleesMorale"/> is rejected before anything is computed; morale 0 is rejected
    /// only <i>after</i> the roll has been made and passed. Same outcome, but the roll is consumed —
    /// so a port that folds them into one early guard desynchronises a shared RNG stream from the
    /// original's.
    /// </remarks>
    public static bool Routs(int staminaPercent, int morale, int rollPercent,
        System.Collections.Generic.IReadOnlyList<int> thresholds, bool isUnderground) {
        if (morale == NeverFleesMorale) {
            return false;
        }
        if (isUnderground) {
            return false;
        }
        if (thresholds == null || thresholds.Count == 0) {
            return false;
        }

        int index = IndexFor(staminaPercent, morale);
        if (index >= thresholds.Count) {
            index = thresholds.Count - 1;
        }

        // Flees when the roll comes in UNDER the threshold, so the table entry is the percentage
        // chance to rout: 85 at the worst end, 0 at the best.
        if (thresholds[index] <= rollPercent) {
            return false;
        }

        // Checked here, not above: morale 0 has already spent the roll.
        return morale != 0;
    }

    /// <summary>
    /// Whether the roll was consumed, whatever the outcome — the ordering above, made checkable.
    /// </summary>
    /// <remarks>
    /// True except for the two guards that return before rolling. A caller sharing an RNG with
    /// anything else needs this to stay in step with the original.
    /// </remarks>
    public static bool ConsumesARoll(int morale, bool isUnderground) =>
        morale != NeverFleesMorale && !isUnderground;
}
