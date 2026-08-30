namespace GameData.Resources.Combat;

using System.Collections.Generic;
using GameData.Resources.Spells;

/// <summary>
/// The AI's support turn — <c>monster_healAnAlly</c> (@0x65bcd) and
/// <c>monster_chooseHealSpell</c> (@0x65b1c); canassa <c>combat_ai_try_cast_heal</c> /
/// <c>combat_ai_pick_heal_spell</c> (CBTAI.C:184, 219).
///
/// <para><b>This is action slot 1 of the caster's priority table, and every pattern row contains
/// it.</b> Pattern 1 tries it first. The other seven slots are the six targeted-cast packets and
/// the wounded-enemy cast; this is the only one that helps its own side, which makes it the whole
/// of the game's monster support AI.</para>
///
/// <para><b>One ally per action.</b> The scan stops at the first eligible recipient — a caster
/// cannot blanket its pack in one turn, however many are hurt.</para>
/// </summary>
/// <remarks>
/// <b>THE TWO SIDES ARE NOT THE SAME LIST, AND THAT IS THE STRANGE PART.</b> The spell is chosen
/// from the health of the side the caster is fighting, and then cast on the side the caster
/// belongs to — <c>pActorTable_B</c> for the decision, <c>pActorTable_A</c> for the recipient
/// (canassa: <c>g_pCombatActiveActors</c> and <c>g_pCombatOtherActors</c>). Both readings of the
/// binary agree on it, so it is transcribed rather than tidied into "look at your allies' health".
/// </remarks>
public static class MonsterHealTurn {
    /// <summary>Nothing to cast.</summary>
    public const int NoSpell = -1;

    /// <summary>Gift of Sung — the restore, chosen when someone is hurt enough.</summary>
    public const int RestoreSpell = SpellIds.GiftOfSung;

    /// <summary>Hocho's Haven — the ward, the fallback when the restore is not called for.</summary>
    public const int WardSpell = SpellIds.HochosHaven;

    /// <summary>
    /// The <c>g_anStatCheckThreshold</c> index the caster's own health must clear.
    /// </summary>
    /// <remarks>
    /// <b>A third distinct index.</b> <see cref="MonsterCasterTurn"/> names 0 on its first pass and
    /// 1 on its retry; this names 2. All three entries happen to hold <b>10</b>, so today they
    /// behave alike — but they are three separate knobs in the shipped table and collapsing them
    /// into one constant would silently couple them.
    /// </remarks>
    public const int HealthGateThresholdIndex = 2;

    /// <summary>Whether the caster is well enough to spend its turn supporting.</summary>
    /// <remarks>Health strictly above the threshold, exactly as the cast gate is.</remarks>
    public static bool WellEnoughToHelp(int casterHealth, IReadOnlyList<int> thresholds) =>
        thresholds != null
        && HealthGateThresholdIndex < thresholds.Count
        && casterHealth > thresholds[HealthGateThresholdIndex];

    /// <summary>The bound of the roll the restore arm is tested against — <c>RND(80)</c>.</summary>
    public const int RestoreRollBound = 80;

    /// <summary>
    /// The spell this turn will use, or <see cref="NoSpell"/>.
    /// </summary>
    /// <param name="opposingHealth">
    /// Health of the actors on the side the caster is FIGHTING, in field order. See the type
    /// remarks: the decision reads this list and the cast lands on the other one.
    /// </param>
    /// <param name="roll"><c>RND(80)</c>.</param>
    /// <param name="restoreCastable"><c>CanSpellBeCast(7, caster, -1)</c>.</param>
    /// <param name="wardCastable"><c>CanSpellBeCast(6, caster, -1)</c>.</param>
    /// <param name="candidateAlreadyWarded">
    /// Whether the FIRST actor of the caster's own side already carries Hocho's Haven — see
    /// <see cref="WardChecksTheProbeNotTheRecipient"/>.
    /// </param>
    /// <remarks>
    /// <b>THE RUNNING MINIMUM IS COMPUTED AND THEN NEVER READ.</b> The loop keeps the lowest health
    /// it has seen in one slot and overwrites another with every actor it examines, and the
    /// decision reads the second one — <b>the LAST actor in the list</b>, not the weakest. Verified
    /// twice over: from the encoded displacements at 0x65b7e in the disassembly, and independently
    /// from the byte-matched C, where the minimum lands in a variable nothing else touches.
    ///
    /// <para>So this is not "heal when someone is badly hurt". It is "heal when the last actor in
    /// the opposing list happens to roll under RND(80)", which is close to a coin flip weighted by
    /// one arbitrary actor's health. A port that implemented the evident intent — the minimum —
    /// would make monster casters markedly better at healing than the shipped game, and the
    /// difference is largest exactly when it matters, with one ally nearly dead.</para>
    /// </remarks>
    public static int ChooseSpell(IReadOnlyList<int> opposingHealth, int roll,
        bool restoreCastable, bool wardCastable, bool candidateAlreadyWarded) {
        if (LastExamined(opposingHealth, out int lastSeen) && lastSeen < roll && restoreCastable) {
            return RestoreSpell;
        }

        return wardCastable && !candidateAlreadyWarded ? WardSpell : NoSpell;
    }

    /// <summary>The health the decision actually reads — the last entry, not the smallest.</summary>
    /// <remarks>
    /// <b>An empty list leaves the original reading an uninitialised stack slot</b>, so there is no
    /// faithful answer for it. This reports "no reading" and the restore arm is skipped, which is
    /// the safe half of undefined behaviour. It is unreachable in a real fight: the opposing side
    /// is non-empty for as long as there is a fight to have.
    /// </remarks>
    public static bool LastExamined(IReadOnlyList<int> opposingHealth, out int lastSeen) {
        lastSeen = 0;
        if (opposingHealth == null || opposingHealth.Count == 0) {
            return false;
        }
        lastSeen = opposingHealth[opposingHealth.Count - 1];
        return true;
    }

    /// <summary>
    /// <b>The ward's "already has it" test reads the PROBE, not whoever receives the spell.</b>
    /// </summary>
    /// <remarks>
    /// <c>getSpellEffectSlot</c> is called on the actor handed to the picker — the first of the
    /// caster's own side — while the recipient is settled afterwards and may be somebody else
    /// entirely. So a monster will happily stack Hocho's Haven on an ally who already has it, as
    /// long as the first actor in the list does not. Wiring the check to the recipient would be
    /// the sensible reading and the wrong one.
    /// </remarks>
    public static bool WardChecksTheProbeNotTheRecipient => true;

    /// <summary>
    /// Whether an actor can receive the spell: not the caster, and hurt but not dead.
    /// </summary>
    /// <param name="healthPercent">
    /// <c>combat_actor_stat_percent(actor, 0)</c> — health as a percentage of its own maximum.
    /// </param>
    /// <remarks>
    /// <b>Strictly between 0 and 100, so a corpse is skipped and so is anyone at full health.</b>
    /// The upper bound is what stops a caster wasting the turn topping up an untouched ally; the
    /// lower bound is why this is a heal and not a resurrection.
    /// </remarks>
    public static bool CanReceive(int healthPercent, bool isCaster) =>
        !isCaster && healthPercent > 0 && healthPercent < 100;

    /// <summary>
    /// The index of the ally to cast on, or -1.
    /// </summary>
    /// <param name="allyHealthPercent">The caster's own side, in field order.</param>
    /// <param name="casterIndex">Where the caster sits in that list, or -1 if it is not in it.</param>
    /// <remarks>
    /// <b>The first actor gets a look of its own before the scan, and the scan then starts from
    /// zero again.</b> That double-checks index 0 and changes nothing, which is why this is one
    /// pass here — the original's shape is an artefact of the probe it also uses to pick the
    /// spell, not a rule about who is favoured.
    /// </remarks>
    public static int PickRecipient(IReadOnlyList<int> allyHealthPercent, int casterIndex) {
        if (allyHealthPercent == null) {
            return -1;
        }
        for (var i = 0; i < allyHealthPercent.Count; i++) {
            if (CanReceive(allyHealthPercent[i], i == casterIndex)) {
                return i;
            }
        }
        return -1;
    }
}
