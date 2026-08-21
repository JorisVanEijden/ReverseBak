namespace GameData.Resources.Combat;

/// <summary>
/// The creature-specific spell reactions that run <b>before</b> anything else on an AI turn —
/// <c>combat_ai_pick_action</c> (<c>SRC/COMBAT/AI/CBTAI.C</c>).
///
/// <para><c>combat_ai_execute_turn</c> calls this first and returns immediately if it fires, so
/// these preempt the morale check's outcome, the class routines and the capability cascade alike.
/// Three passes, each looking for one group of creature types on the field and casting one specific
/// spell at the first one it settles on.</para>
///
/// <para><b>SETTLED: the scan walks the CASTER'S OWN SIDE.</b> It reads
/// <c>g_pCombatActiveActors</c>, which <c>combat_arena_swap_tgt_state</c> re-aims when the turn
/// changes hands, so the pointer alone does not say which side it is. The CONTENT does: every pass
/// hunts specific monster creature types, and one of the two actions is a raise-dead on a fallen
/// Black Slayer. You do not resurrect an enemy. So the candidate list is the AI's allies.</para>
///
/// <para><b>The list stays a parameter anyway, and that is not hedging.</b> The same
/// <c>g_pCombatActiveActors</c> is read as the ENEMY list elsewhere in the same subsystem —
/// <c>CBTAITRN.C</c> assigns <c>actor->inner->target</c> straight out of it — so the array genuinely
/// means different sides at different points in the swap cycle. A model that hard-coded "allies"
/// into the reader would be right here and wrong there; keeping it a parameter is what lets both
/// callers be correct.</para>
/// </summary>
public static class OpportunisticCasts {
    /// <summary>Black Slayer, risen form.</summary>
    public const int BlackSlayerRisen = 0x16;

    /// <summary>Black Slayer, transforming form.</summary>
    public const int BlackSlayerTransforming = 0x17;

    /// <summary>The type the second pass looks for.</summary>
    public const int SecondPassType = 0x36;

    /// <summary>The types the third pass looks for.</summary>
    public static readonly int[] ThirdPassTypes = { 0x29, 0x2a, 0x2b };

    /// <summary>Cast at a living Black Slayer that a projectile can reach.</summary>
    public const int SpellAtLivingSlayer = 9;

    /// <summary>Cast at a fallen Black Slayer still on the grid — the raise.</summary>
    public const int SpellAtFallenSlayer = 0x20;

    /// <summary>The second pass's spell.</summary>
    public const int SecondPassSpell = 0x2a;

    /// <summary>The third pass's spell.</summary>
    public const int ThirdPassSpell = 0x29;

    /// <summary>Nothing to cast.</summary>
    public const int NoSpell = -1;

    /// <summary>
    /// <b>Each pass stops at the first matching creature only ~90% of the time.</b>
    /// </summary>
    /// <remarks>
    /// The scan runs <c>if (RND(100) &gt; 10) break;</c> on every match, so a roll of 0..10 walks
    /// PAST that creature and keeps looking. With one such creature on the field the pass therefore
    /// does nothing at all about one time in ten — it is a chance to skip, not a chance to act, and
    /// implementing it as "act with 90% probability" gets the multi-creature case wrong.
    /// </remarks>
    public static bool StopsAtThisMatch(int rollUnder100) => rollUnder100 > SkipRollCeiling;

    /// <inheritdoc cref="StopsAtThisMatch"/>
    public const int SkipRollCeiling = 10;

    /// <summary>What one candidate offers, as the passes see it.</summary>
    public sealed class Candidate {
        public int CreatureType { get; set; }

        /// <summary><see cref="CombatantFlags.Dead"/>.</summary>
        public bool IsDead { get; set; }

        /// <summary><see cref="CombatantFlags.Fleeing"/> — a creature that ran is not raised.</summary>
        public bool IsFleeing { get; set; }

        /// <summary>Whether it still occupies a tile; off-grid is <c>gridX == -1</c>.</summary>
        public bool IsOnGrid { get; set; } = true;

        /// <summary>Whether a projectile path traces from the caster to it.</summary>
        public bool ProjectilePathIsClear { get; set; }
    }

    /// <summary>
    /// The spell the first pass would cast at this Black Slayer, or <see cref="NoSpell"/>.
    /// </summary>
    /// <remarks>
    /// Two arms, and the live one <b>excludes the transforming form</b>: a living
    /// <see cref="BlackSlayerRisen"/> with a clear path takes
    /// <see cref="SpellAtLivingSlayer"/>, while a <see cref="BlackSlayerTransforming"/> does not,
    /// even though the scan finds it. The fallen arm takes either form, provided it did not flee and
    /// is still on the grid — a creature that left the field cannot be raised, which is the same
    /// double bar the revival sweep applies.
    /// </remarks>
    public static int FirstPassSpellFor(Candidate candidate, bool livingSpellCastable,
        bool fallenSpellCastable) {
        if (candidate == null) {
            return NoSpell;
        }

        if (!candidate.IsDead && livingSpellCastable && candidate.ProjectilePathIsClear
            && candidate.CreatureType != BlackSlayerTransforming) {
            return SpellAtLivingSlayer;
        }

        if (candidate.IsDead && fallenSpellCastable && !candidate.IsFleeing && candidate.IsOnGrid) {
            return SpellAtFallenSlayer;
        }

        return NoSpell;
    }

    /// <summary>Whether a creature type is one the first pass looks for.</summary>
    public static bool IsFirstPassType(int creatureType) =>
        creatureType == BlackSlayerRisen || creatureType == BlackSlayerTransforming;

    /// <summary>Whether a creature type is one the third pass looks for.</summary>
    public static bool IsThirdPassType(int creatureType) {
        foreach (int t in ThirdPassTypes) {
            if (t == creatureType) {
                return true;
            }
        }
        return false;
    }

    /// <summary>The second pass's spell for this candidate, or <see cref="NoSpell"/>.</summary>
    /// <remarks>Only a living one; unlike the third pass it does not test fleeing.</remarks>
    public static int SecondPassSpellFor(Candidate candidate, bool castable) =>
        candidate != null && !candidate.IsDead && castable ? SecondPassSpell : NoSpell;

    /// <summary>The third pass's spell for this candidate, or <see cref="NoSpell"/>.</summary>
    /// <remarks>Living <b>and</b> not fleeing — the extra bar the second pass does not have.</remarks>
    public static int ThirdPassSpellFor(Candidate candidate, bool castable) =>
        candidate != null && !candidate.IsDead && !candidate.IsFleeing && castable
            ? ThirdPassSpell
            : NoSpell;
}
