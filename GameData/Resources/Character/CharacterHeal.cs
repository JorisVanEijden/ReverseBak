namespace GameData.Resources.Character;

using System;
using System.Collections.Generic;

/// <summary>
/// The heal a dialog or chapter transition hands out — <c>stat_combatant_heal</c>
/// (<c>SRC/CHAR/STAT.C</c>) and its party-wide wrapper <c>stat_party_heal_all</c>.
///
/// <para><b>The amount is not a magnitude.</b> It reads like "heal this much" and is nothing of the
/// sort. The pool is always filled outright; the amount only picks which of two extras apply, and
/// the two are tested against <i>different</i> comparisons, which gives three behaviours:</para>
/// <list type="bullet">
/// <item><b>Exactly 100</b> — fills, and also cures every affliction.</item>
/// <item><b>Below 100</b> — fills, then gives 20% back, landing at 80% of maximum. Every value
/// below 100 behaves identically; the number itself is never used.</item>
/// <item><b>Above 100</b> — fills and stops. No cure (that needs exactly 100) and no giving back
/// (that needs below 100), so it is the most generous input of the three.</item>
/// </list>
/// <para>Treating the amount as hit points to add would be wrong for every caller in the game.</para>
/// </summary>
public static class CharacterHeal {
    /// <summary>The one value that means "full restore, and cure everything".</summary>
    public const int FullHealAmount = 100;

    /// <summary>What a partial heal leaves behind, as a percentage of the maximum pool.</summary>
    public const int PartialHealPercent = 80;

    /// <summary>Delta large enough to fill the pool to its target in one go (the original's 0x7fff).</summary>
    private const int FillDelta = 0x7fff;

    private const int TakeBackPercent = 20;

    /// <summary>
    /// Heals one character.
    /// </summary>
    /// <returns>
    /// True when this was a full heal. The caller should then stamp the party's last-rest time,
    /// which the original does inside this function — it lives outside because the timestamp is
    /// party state, not character state.
    /// </returns>
    public static bool Apply(ActorStat[] stats, ActorConditions conditions, int amount) {
        if (stats == null) {
            throw new ArgumentNullException(nameof(stats));
        }
        ActorStat health = stats[(int)ActorAttribute.Health];
        ActorStat stamina = stats[(int)ActorAttribute.Stamina];

        // The two extras are gated on different comparisons in the original — == 100 for the cure,
        // < 100 for the give-back — so an amount above 100 gets neither. Keying both off "is it
        // 100" would quietly take a fifth back off every heal above 100.
        bool full = amount == FullHealAmount;
        bool givesBack = amount < FullHealAmount;
        if (full && conditions != null) {
            // Every affliction, driven down by more than its maximum so all of them clear.
            for (var i = 0; i < ActorConditions.Count; i++) {
                ConditionEngine.Apply(conditions, (ActorCondition)i, -FullHealAmount);
            }
        }

        int nearDeath = conditions?[ActorCondition.NearDeath] ?? 0;
        StatEngine.ModifyHealthPool(health, stamina, FillDelta, 100, out _, nearDeath);

        if (givesBack) {
            // Fill, then give a fifth of it back — which is why every amount below 100 lands in the
            // same place regardless of what the value actually was.
            int pool = health.Base + stamina.Base;
            long delta = -(long)pool * TakeBackPercent / 100;
            StatEngine.ModifyHealthPool(health, stamina, delta << 8, 100, out _, nearDeath);
        }
        return full;
    }

    /// <summary>
    /// Heals everyone in the active party — <c>stat_party_heal_all</c>.
    ///
    /// <para>It walks the <b>active roster</b>, not every character record, so a member sitting out
    /// is not healed. Each member gets the identical amount, with all the amount's quirks above.</para>
    /// </summary>
    /// <returns>True when this was a full heal, so the caller can stamp the last-rest time once.</returns>
    public static bool ApplyToParty(IEnumerable<(ActorStat[] Stats, ActorConditions Conditions)> party,
        int amount) {
        if (party == null) {
            throw new ArgumentNullException(nameof(party));
        }
        foreach ((ActorStat[] stats, ActorConditions conditions) in party) {
            if (stats != null) {
                Apply(stats, conditions, amount);
            }
        }
        return amount == FullHealAmount;
    }
}
