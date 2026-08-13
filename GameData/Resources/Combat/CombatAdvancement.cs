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
    /// The award for a successful cast — <c>cspell_resolve_cast</c> pays the caster's
    /// AccuracyCasting on a hit, the same one-point skill-use award.
    /// </summary>
    public static void OnSpellHit(ActorStat casterCasting) {
        Award(casterCasting, ActorAttribute.AccuracyCasting);
    }

    private static void Award(ActorStat stat, ActorAttribute attribute) {
        if (stat != null) {
            StatEngine.Modify(stat, attribute, AwardDelta, StatChangeMode.SkillUse);
        }
    }
}
