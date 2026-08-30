namespace GameData.Resources.Combat;

using System;
using System.Collections.Generic;

/// <summary>
/// The creature-specific spell reactions that run <b>before</b> anything else on an AI turn —
/// <c>combat_ai_pick_action</c> (<c>SRC/COMBAT/AI/CBTAI.C</c>).
///
/// <para><c>combat_ai_execute_turn</c> calls this first and returns immediately if it fires, so
/// these preempt the morale check's outcome, the class routines and the capability cascade alike.
/// Three passes, each looking for one group of creature types on the field and casting one specific
/// spell at the first one it settles on.</para>
///
/// <para><b>SETTLED: the scan walks the ACTOR'S ENEMIES, and all three passes cast HOSTILE
/// spells.</b> This file previously said the opposite — "the caster's own side", marked settled —
/// on the strength of a guessed spell identity, and a caller that believed it would have had
/// monsters buffing the party. Four independent checks agree on the correction:</para>
///
/// <list type="number">
/// <item><c>combat_ai_try_cast_heal</c> (CBTAI.C:219) scans <c>g_pCombatOtherActors</c> for the
/// wounded and heals them, skipping the caster itself. A heal goes to your own side, so
/// <b>Other</b> is the actor's own side and <c>g_pCombatActiveActors</c> — the array THIS routine
/// reads — is the enemy list.</item>
/// <item><c>combatenc_is_encounter_actor</c> tests membership of <c>g_pCombatOtherActors</c>, and
/// the turn loop at COMBAT.C:1445 runs the AI for exactly the actors it accepts — so during an
/// ordinary monster turn Other holds the monsters.</item>
/// <item>The party-side AI case swaps first: <c>if (flags &amp; CAF_AI_SUMMON) { swap;
/// combatenc_ai_run_turn(); swap; }</c> (COMBAT.C:2278). The swap exists to keep "Active = my
/// enemies" true for an actor on the other side.</item>
/// <item>The spells, which is the check that makes it unarguable — see the constants below.</item>
/// </list>
///
/// <para><b>The list stays a parameter anyway, and that is not hedging.</b> The same array is the
/// caster's own side one swap later, which is how auto-resolve plays the party through this very
/// routine. A model that hard-coded a side would be right for one caller and wrong for the other.
/// What the correction changes is only which list the CALLER hands in.</para>
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

    /// <summary>
    /// Cast at a LIVING Black Slayer a projectile can reach — spell 9, <b>Bane of Black
    /// Slayers</b> (damage 5, targeting type 0).
    /// </summary>
    /// <remarks>
    /// The name is the proof that this pass hunts enemies: the game ships a spell whose entire
    /// purpose is killing Black Slayers, and this is the routine that reaches for it.
    /// </remarks>
    public const int SpellAtLivingSlayer = 9;

    /// <summary>
    /// Cast at a FALLEN Black Slayer still on the grid — spell 0x20, <b>Final Rest</b>.
    /// </summary>
    /// <remarks>
    /// <b>It is not a raise, and reading it as one is what inverted this file's side claim.</b>
    /// Final Rest is the counter to <see cref="SlayerRevival"/>: a downed Nighthawk gets back up
    /// as a Black Slayer after its countdown, and this lays the corpse to rest before it can. So
    /// the fallen arm is aimed at an enemy corpse for the same reason the living arm is aimed at
    /// an enemy — one stops it now, the other stops it coming back.
    ///
    /// <para>That is also why the arm insists the body is still ON the grid and did not flee: the
    /// same double bar the revival sweep applies, because those are exactly the corpses that can
    /// rise.</para>
    /// </remarks>
    public const int SpellAtFallenSlayer = 0x20;

    /// <summary>The second pass's spell — 0x2a, <b>Strength Drain</b>.</summary>
    public const int SecondPassSpell = 0x2a;

    /// <summary>The third pass's spell — 0x29, <b>Thy Master's Will</b>.</summary>
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

    /// <summary>What the three passes settled on, or nothing.</summary>
    public readonly struct Cast {
        /// <summary>The spell to cast, or <see cref="NoSpell"/>.</summary>
        public readonly int SpellId;

        /// <summary>Index into the candidate list, or -1.</summary>
        public readonly int TargetIndex;

        public Cast(int spellId, int targetIndex) {
            SpellId = spellId;
            TargetIndex = targetIndex;
        }

        public bool Fires => SpellId != NoSpell;
    }

    /// <summary>
    /// Run the three passes over <paramref name="enemies"/> and return the first cast that
    /// commits — <c>combat_ai_pick_action</c> end to end.
    /// </summary>
    /// <param name="enemies">
    /// The acting actor's OPPONENTS, in field order. See the type summary for why this is the
    /// enemy list and not the ally list, and why it stays a parameter regardless.
    /// </param>
    /// <param name="rnd"><c>RND(n)</c>.</param>
    /// <param name="castable">
    /// <c>cspell_check_castable(spell, actor, 0)</c> — whether this actor can afford and knows
    /// the given spell right now.
    /// </param>
    /// <remarks>
    /// <b>A pass that FINDS someone but cannot cast at them does not stop the scan.</b> Only the
    /// first pass is written with an early exit in the original, and even it falls through to the
    /// second when neither of its two arms produces a spell (<c>goto L_phase2</c>). Returning
    /// "nothing" the moment a pass matched a creature would silence the two later passes whenever
    /// a Black Slayer happened to be standing on the field.
    ///
    /// <para><b>Each pass rolls its own skip.</b> The scan is not "find the first match" — it is
    /// "walk until a match survives its roll" (<see cref="StopsAtThisMatch"/>), so with several
    /// matching creatures on the field the pass may settle on the second or third rather than the
    /// nearest, and with one it may settle on nobody at all.</para>
    /// </remarks>
    public static Cast Choose(IReadOnlyList<Candidate> enemies, Func<int, int> rnd,
        Func<int, bool> castable) {
        if (enemies == null || enemies.Count == 0) {
            return new Cast(NoSpell, -1);
        }
        Func<int, int> roll = rnd ?? (_ => 100);
        Func<int, bool> can = castable ?? (_ => false);

        int i = Settle(enemies, IsFirstPassType, roll);
        if (i >= 0) {
            int spell = FirstPassSpellFor(enemies[i], can(SpellAtLivingSlayer),
                can(SpellAtFallenSlayer));
            if (spell != NoSpell) {
                return new Cast(spell, i);
            }
        }

        i = Settle(enemies, t => t == SecondPassType, roll);
        if (i >= 0) {
            int spell = SecondPassSpellFor(enemies[i], can(SecondPassSpell));
            if (spell != NoSpell) {
                return new Cast(spell, i);
            }
        }

        i = Settle(enemies, IsThirdPassType, roll);
        if (i >= 0) {
            int spell = ThirdPassSpellFor(enemies[i], can(ThirdPassSpell));
            if (spell != NoSpell) {
                return new Cast(spell, i);
            }
        }

        return new Cast(NoSpell, -1);
    }

    /// <summary>
    /// Walk the list for a creature of the wanted kind whose skip roll fails, or -1.
    /// </summary>
    /// <remarks>
    /// The original's loop breaks out with the index still in scope and then tests
    /// <c>i &lt; count</c>, which is the same thing as "-1 for nobody" — written that way here
    /// because a C for-loop's escaped counter does not survive the translation.
    /// </remarks>
    private static int Settle(IReadOnlyList<Candidate> enemies, Func<int, bool> wanted,
        Func<int, int> roll) {
        for (var i = 0; i < enemies.Count; i++) {
            Candidate c = enemies[i];
            if (c != null && wanted(c.CreatureType) && StopsAtThisMatch(roll(100))) {
                return i;
            }
        }
        return -1;
    }

    /// <summary>The third pass's spell for this candidate, or <see cref="NoSpell"/>.</summary>
    /// <remarks>Living <b>and</b> not fleeing — the extra bar the second pass does not have.</remarks>
    public static int ThirdPassSpellFor(Candidate candidate, bool castable) =>
        candidate != null && !candidate.IsDead && !candidate.IsFleeing && castable
            ? ThirdPassSpell
            : NoSpell;
}
