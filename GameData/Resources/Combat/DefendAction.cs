namespace GameData.Resources.Combat;

using System;

/// <summary>
/// The combat HUD's Defend button — <c>combatenc_actor_enter_defense</c> (canassa CBENC.C:796).
///
/// <para><b>Defending is not only a stance: it also heals.</b> A port that just raises a guard flag
/// drops the recovery, which is the whole reason a player spends a turn on it.</para>
/// </summary>
public static class DefendAction {
    /// <summary>
    /// <b>Defend sets <see cref="CombatantFlags.DefendCommand"/>, NOT
    /// <see cref="CombatantFlags.Parry"/>.</b>
    /// </summary>
    /// <remarks>
    /// The two are different flags (0x04 and 0x08) and only Parry feeds the to-hit penalty in
    /// <see cref="CombatFormulas.MeleeHits"/>. Conflating them would hand every defending character a
    /// melee bonus the original never gives.
    /// </remarks>
    public static CombatantFlags FlagSet => CombatantFlags.DefendCommand;

    /// <summary>The divisor behind the recovery — <c>sumMax / 0x1e</c>.</summary>
    public const int HealDivisor = 30;

    /// <summary>Help text shown when the button is right-clicked instead of pressed.</summary>
    /// <remarks>
    /// Every combat button has a preview branch that plays a dialog and returns without acting
    /// (COMBAT.C ~2010). Defend's is 0x107; Shoot's is 0x108.
    /// </remarks>
    public const int HelpDialog = 0x107;

    /// <summary>
    /// The attribute the recovery is applied to.
    /// </summary>
    /// <remarks>
    /// <c>stat_combatant_modify(actor, 0x10, ...)</c> — attribute 16, the combined health/stamina
    /// entry rather than either one alone.
    /// </remarks>
    public const ActorAttribute HealedAttribute = ActorAttribute.HealthStaminaCombo;

    /// <summary>
    /// How much a defending character recovers.
    /// </summary>
    /// <param name="maxHealth">Health's ceiling.</param>
    /// <param name="maxStamina">Stamina's ceiling.</param>
    /// <remarks>
    /// <b>Off the CEILINGS, not the current values</b>, so a badly wounded character recovers the
    /// same amount as a fresh one — the rate depends on who you are, not on how hurt you are.
    ///
    /// <para><b>At least 1.</b> The floor matters: any character whose combined maxima are under 30
    /// would otherwise recover nothing at all and defending would be a wasted turn for them.</para>
    /// </remarks>
    public static int HealAmount(int maxHealth, int maxStamina) =>
        Math.Max(1, (maxHealth + maxStamina) / HealDivisor);

    /// <summary>
    /// Spend the turn defending.
    /// </summary>
    /// <param name="actor">The acting combatant.</param>
    /// <param name="recovers">
    /// Whether the recovery applies. <b>The original gates it on a per-character table this model
    /// does not yet identify</b> — a monster (no character slot) always recovers, while a party
    /// member recovers only when six bytes of a per-character table are all zero. canassa calls that
    /// table <c>aSkillTrainRate</c>, but its indexing there (offsets 5..11 with a stride of 7)
    /// overruns one character's row, so the name and the layout cannot both be right. Passed in
    /// rather than guessed.
    /// </param>
    /// <returns>The amount recovered, or 0 when it does not apply.</returns>
    /// <remarks>
    /// <b>Defending ends the turn.</b> Ready is cleared and the acting speed is spent, so this is a
    /// commitment rather than a free stance.
    /// </remarks>
    public static int Apply(Combatant actor, bool recovers, int maxHealth, int maxStamina) {
        if (actor == null) {
            return 0;
        }

        actor.Flags |= FlagSet;
        actor.Flags &= ~CombatantFlags.Ready;
        return recovers ? HealAmount(maxHealth, maxStamina) : 0;
    }
}
