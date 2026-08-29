namespace GameData.Resources.Combat;

using GameData;
using GameData.Resources.Character;

/// <summary>
/// How fighting improves a character — <c>combat_arena_melee_attack</c>
/// (<c>SRC/COMBAT/ARENA/COMBAT.C</c>).
///
/// <para><b>There is no experience for a kill.</b> Nothing in the death handler awards anything,
/// and the word "experience" does not appear anywhere in the combat sources: advancement is
/// entirely use-based and is paid out during the <i>exchange</i>, not for the corpse. Killing a
/// creature is worth exactly the swings it took to kill it, and no more. A port that adds kill XP
/// is inventing a system the game does not have.</para>
///
/// <para>Every award here is a single point through <see cref="StatChangeMode.SkillUse"/>, which
/// banks the sub-unit remainder — so it is the repetition that advances a skill, not any one
/// swing.</para>
/// </summary>
public static class CombatAdvancement {
    /// <summary>The one-point delta every combat award uses.</summary>
    public const int AwardDelta = 1;

    /// <summary>
    /// The pair of awards made the moment a melee attack is declared, before any roll:
    /// the <b>defender</b> improves Defense and the <b>attacker</b> improves Melee.
    ///
    /// <para><b>Being attacked trains you.</b> The defender is paid for standing there, win or
    /// lose, which is why a character who only ever gets hit still improves at not being hit. The
    /// attacker is paid for swinging whether or not the swing lands.</para>
    /// </summary>
    /// <param name="defenderDefense">The defender's Defense stat, or null to skip.</param>
    /// <param name="attackerMelee">The attacker's AccuracyMelee stat, or null to skip.</param>
    public static void OnMeleeDeclared(ActorStat defenderDefense, ActorStat attackerMelee) {
        Award(defenderDefense, ActorAttribute.Defense);
        Award(attackerMelee, ActorAttribute.AccuracyMelee);
    }

    /// <summary>
    /// The awards made when a melee swing actually connects: the attacker improves Melee
    /// <b>again</b> and also improves Strength.
    ///
    /// <para>So a landed hit pays the attacker twice over for Melee — once for trying, once for
    /// connecting — which is the whole of the "fighting makes you better at fighting" curve.</para>
    /// </summary>
    public static void OnMeleeHit(ActorStat attackerMelee, ActorStat attackerStrength) {
        Award(attackerMelee, ActorAttribute.AccuracyMelee);
        Award(attackerStrength, ActorAttribute.Strength);
    }

    /// <summary>
    /// The award made for <b>taking a shot at all</b>, before any roll — the shooter improves
    /// AccuracyCrossbow.
    /// </summary>
    /// <remarks>
    /// <b>The defender is paid NOTHING for being shot at.</b> Melee pays both sides on declaration
    /// (see <see cref="OnMeleeDeclared"/>) and the ranged routine pays only the shooter — which
    /// follows from <see cref="CombatFormulas.RangedHitChance"/> ignoring the target's defence
    /// entirely: there is no defensive skill in play to improve. A port that mirrored the melee pair
    /// would train Defense off arrows that defence never affected.
    /// </remarks>
    public static void OnShotDeclared(ActorStat shooterCrossbow) {
        Award(shooterCrossbow, ActorAttribute.AccuracyCrossbow);
    }

    /// <summary>
    /// The <b>second</b> AccuracyCrossbow award, made when the shot lands.
    /// </summary>
    /// <remarks>
    /// Same once-for-trying, once-for-connecting shape melee and casting use —
    /// <see cref="RangedExchange.SkillAwards"/> counts the pair. <b>No Strength award</b>: ranged
    /// damage has no Strength term (<see cref="CombatFormulas.RangedDamage"/>), so there is nothing
    /// for it to train.
    /// </remarks>
    public static void OnShotHit(ActorStat shooterCrossbow) {
        Award(shooterCrossbow, ActorAttribute.AccuracyCrossbow);
    }

    /// <summary>
    /// The award made for <b>casting at all</b>, before any roll — the caster improves
    /// AccuracyCasting.
    /// </summary>
    /// <remarks>
    /// Unconditional, exactly as the attacker's award in <see cref="OnMeleeDeclared"/> is. A cast
    /// that misses still teaches you something.
    /// </remarks>
    public static void OnSpellCast(ActorStat casterCasting) {
        Award(casterCasting, ActorAttribute.AccuracyCasting);
    }

    /// <summary>
    /// The <b>second</b> AccuracyCasting award, made when the cast connects.
    /// </summary>
    /// <remarks>
    /// <b>Casting pays the same way melee does: once for trying, once for connecting.</b> This file
    /// previously described the spell award as a single hit-only payment, which under-rewarded every
    /// successful cast by half and paid nothing at all for a miss. Corrected 2026-08-14 from
    /// <c>Cast_Spell</c> @0x68617, where the unconditional award is followed by a second one guarded
    /// on the hit flag.
    ///
    /// <para>Note it is only the delivery categories that reach this pair — the ones that play the
    /// windup animation. Others take a different branch.</para>
    /// </remarks>
    public static void OnSpellHit(ActorStat casterCasting) {
        Award(casterCasting, ActorAttribute.AccuracyCasting);
    }

    private static void Award(ActorStat stat, ActorAttribute attribute) {
        if (stat != null) {
            StatEngine.Modify(stat, attribute, AwardDelta, StatChangeMode.SkillUse);
        }
    }
}
