namespace GameData.Resources.Combat;

/// <summary>
/// Which melee attack a monster makes — <c>combatenc_ai_melee_pick</c> (canassa CBENC.C:829).
/// </summary>
/// <remarks>
/// <b>The AI picks between the same two attacks the player has on the two mouse buttons</b>, and it
/// is not a coin flip: it rolls d100 against its own <i>swing</i> accuracy and swings only if the
/// roll lands — otherwise it thrusts. So a creature that would probably miss with the heavy attack
/// makes the light one instead, and a creature with a good weapon swings most of the time.
///
/// <para><b>A port that always calls one routine loses half the monster's melee</b>, and loses it
/// asymmetrically: the swing and the thrust read different weapon fields
/// (<see cref="CombatActionDispatch.AccuracyOf"/>), wear the weapon by different amounts and — for
/// the player's copy of the same choice — have different reach.</para>
/// </remarks>
public static class MonsterMeleeChoice {
    /// <summary>
    /// The speed a swing costs. Below it the monster thrusts however good its roll was.
    /// </summary>
    /// <remarks>
    /// <c>if (g_acting_actor_speed &lt; 2) goto thrust</c> — the AI's counterpart of the player's
    /// <see cref="CombatActionDispatch.SwingMinimumPool"/> gate, and a different quantity: the
    /// player's is a health+stamina pool, this is the movement allowance left in the turn.
    /// </remarks>
    public const int SwingMinimumSpeed = 2;

    /// <summary>
    /// The attack to make.
    /// </summary>
    /// <param name="roll">A roll in <c>[0, 100)</c>.</param>
    /// <param name="accuracyMelee">The attacker's melee accuracy — <c>stat_actor_get(actor, 6, 0)</c>.</param>
    /// <param name="swingAccuracy">The weapon's swing accuracy field.</param>
    /// <param name="speedLeft">Movement allowance remaining this turn.</param>
    /// <remarks>
    /// <b>Strictly greater loses</b>: the original is <c>if (rand_roll &gt; total) thrust</c>, so a
    /// roll exactly equal to the accuracy still swings.
    ///
    /// <para>The routine dereferences the weapon record without checking it, which is only safe
    /// because a monster in melee has one. An unarmed attacker contributes 0 here rather than
    /// reading rubbish.</para>
    /// </remarks>
    public static CombatActionDispatch.MeleeAttack Pick(
        int roll, int accuracyMelee, int swingAccuracy, int speedLeft) {
        if (roll > accuracyMelee + swingAccuracy || speedLeft < SwingMinimumSpeed) {
            return CombatActionDispatch.MeleeAttack.Thrust;
        }
        return CombatActionDispatch.MeleeAttack.Swing;
    }
}
