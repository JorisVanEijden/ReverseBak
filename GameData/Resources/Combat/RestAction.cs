namespace GameData.Resources.Combat;

using System;

/// <summary>
/// The combat HUD's REST button (action id 19) — <c>combatenc_actor_enter_defense</c>
/// (canassa CBENC.C:796), whose name is the misleading one.
///
/// <para><b>Resting is not a stance at all: it heals.</b> The game's own describe record for id 19
/// (DDX 263) says "causes the current character to rest for one round". Defending is a DIFFERENT
/// button — id 32, <see cref="CombatCommands.Command.Defend"/> — which sets
/// <see cref="CombatantFlags.Parry"/> instead. Do not merge the two.</para>
/// </summary>
public static class RestAction {
    /// <summary>
    /// <b>Rest sets <see cref="CombatantFlags.DefendCommand"/>, NOT
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

    /// <summary>
    /// The ceiling the recovery may reach, as a percentage — <c>stat_combatant_modify</c>'s fourth
    /// argument, 0x50.
    /// </summary>
    /// <remarks>
    /// <b>Defending does not heal to full.</b> 80% is the cap, so a badly hurt character can defend
    /// repeatedly and still never reach their maximum by this route alone. Reading the argument as a
    /// duration — its more natural shape for a combat action — would drop the cap entirely.
    /// </remarks>
    public const int HealCapPercent = 0x50;

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
    /// Whether a character's condition lets them recover from resting.
    /// </summary>
    /// <param name="conditionRanks">
    /// The character's seven condition ranks, indexed by <see cref="ActorCondition"/>.
    /// </param>
    /// <remarks>
    /// <b>Every affliction blocks the recovery; being under Healing does not.</b> The original tests
    /// six bytes of a per-character row and skips exactly one — and the skipped slot is
    /// <see cref="ActorCondition.Healing"/>, the only entry of the seven that is a benefit rather
    /// than an ailment. So resting restores nothing to a character who is sick, plagued, poisoned,
    /// drunk, starving or near death, while a character being healed still recovers.
    ///
    /// <para><b>How the table was identified (2026-08-22).</b> canassa reads
    /// <c>(char *)g_gameState.aSkillTrainRate + N + charSlot * 7</c> for N in 5, 6, 7, 8, 10, 11 —
    /// which cannot be that field, since <c>aSkillTrainRate</c> is an array of SHORTS and a stride of
    /// 7 does not align with it. The struct has exactly one per-character row of seven bytes right
    /// after it, <c>abActorStatusRanks[..][7]</c>, so the decompiler attributed a base+displacement
    /// to the neighbouring field. The offsets clinch it: they are consecutive except for a gap where
    /// 9 would be, i.e. six of seven slots with the fifth skipped — and the fifth condition is
    /// Healing.</para>
    ///
    /// <para><b>A monster always recovers</b>: it has no character slot, so the original skips the
    /// test entirely.</para>
    /// </remarks>
    public static bool RecoveryAllowed(System.Collections.Generic.IReadOnlyList<int> conditionRanks) {
        if (conditionRanks == null) {
            return true;   // no character row to consult — the monster case
        }
        for (var i = 0; i < conditionRanks.Count; i++) {
            if (i == (int)ActorCondition.Healing) {
                continue;
            }
            if (conditionRanks[i] != 0) {
                return false;
            }
        }
        return true;
    }

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
    /// Whether the recovery applies — see <see cref="RecoveryAllowed"/>, which computes it.
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
