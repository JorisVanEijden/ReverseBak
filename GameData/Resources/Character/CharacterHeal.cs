namespace GameData.Resources.Character;

using System;

/// <summary>
/// The heal a dialog or chapter transition hands out — <c>stat_combatant_heal</c>
/// (<c>SRC/CHAR/STAT.C</c>) and its party-wide wrapper <c>stat_party_heal_all</c>.
///
/// <para><b>The amount is not a magnitude.</b> It reads like "heal this much" and is nothing of the
/// sort: exactly 100 means a full restore that also cures every affliction, and <i>any other value</i>
/// produces the same result as every other non-100 value — the pool is filled and then 20% is taken
/// back off, landing at 80% of maximum. Treating it as hit points to add would be wrong for every
/// caller in the game.</para>
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

        bool full = amount == FullHealAmount;
        if (full && conditions != null) {
            // Every affliction, driven down by more than its maximum so all of them clear.
            for (var i = 0; i < ActorConditions.Count; i++) {
                ConditionEngine.Apply(conditions, (ActorCondition)i, -FullHealAmount);
            }
        }

        int nearDeath = conditions?[ActorCondition.NearDeath] ?? 0;
        StatEngine.ModifyHealthPool(health, stamina, FillDelta, 100, out _, nearDeath);

        if (!full) {
            // Fill, then give a fifth of it back — which is why every non-100 amount lands in the
            // same place regardless of what the value actually was.
            int pool = health.Base + stamina.Base;
            long delta = -(long)pool * TakeBackPercent / 100;
            StatEngine.ModifyHealthPool(health, stamina, delta << 8, 100, out _, nearDeath);
        }
        return full;
    }
}
