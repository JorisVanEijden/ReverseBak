namespace GameData.Resources.Character;

using System;

/// <summary>
/// The per-affliction constants the engine drives everything from — the DOS
/// <c>g_aConditionInfo[7]</c> table (canassa <c>SRC/CHAR/STAT.C</c>).
/// </summary>
public readonly struct ConditionInfo {
    public ConditionInfo(string name, int hourlyDelta, int regenDelta,
        int firstMask, int firstPercent, int secondMask, int secondPercent) {
        Name = name;
        HourlyDelta = hourlyDelta;
        RegenDelta = regenDelta;
        FirstMask = firstMask;
        FirstPercent = firstPercent;
        SecondMask = secondMask;
        SecondPercent = secondPercent;
    }

    /// <summary>The engine's own label for the affliction.</summary>
    public string Name { get; }

    /// <summary>
    /// What the rank does to itself each hour, untreated. Positive means it gets worse on its own
    /// (the three diseases); negative means it wears off (Drunk, Healing).
    /// </summary>
    public int HourlyDelta { get; }

    /// <summary>What carrying this affliction adds to the actor's hourly health regeneration.</summary>
    public int RegenDelta { get; }

    /// <summary>Attribute mask for the first read-time penalty (0 = none).</summary>
    public int FirstMask { get; }

    /// <summary>Percentage swing applied at rank 100 for <see cref="FirstMask"/>.</summary>
    public int FirstPercent { get; }

    /// <summary>Attribute mask for the second read-time penalty (0 = none).</summary>
    public int SecondMask { get; }

    /// <summary>Percentage swing applied at rank 100 for <see cref="SecondMask"/>.</summary>
    public int SecondPercent { get; }
}

/// <summary>
/// Afflictions: what they do to an actor's attributes, how they change on their own, and what
/// happens when one is applied. Faithful port of <c>stat_combatant_apply_delta</c>
/// (<c>UpdateActorCondition</c>, 0x43600, VERIFIED) and the <c>g_aConditionInfo</c> table, with the
/// read-time penalty step lifted out of <c>stat_actor_get</c>.
///
/// <para>Afflictions are ranks (0..100), not switches — see <see cref="ActorConditions"/>. They
/// only exist for party members; the original indexes them by party slot, so an actor outside the
/// party has nowhere to store one.</para>
/// </summary>
public static class ConditionEngine {
    // g_aConditionInfo[7]. Only Drunk carries read-time penalties. The mask values are the
    // original's verbatim: they are declared as SHORTS and are negative, so when the engine tests
    // them against an attribute bit it sign-extends — see AppliesTo.
    private static readonly ConditionInfo[] Table = {
        new ConditionInfo("Sick", 1, -1, 0, 0, 0, 0),
        new ConditionInfo("Plagued", 1, -2, 0, 0, 0, 0),
        new ConditionInfo("Poisoned", 1, -3, 0, 0, 0, 0),
        new ConditionInfo("Drunk", -2, 0, -14, -60, 0, 0),
        new ConditionInfo("Healing", -3, 1, 0, 0, 0, 0),
        new ConditionInfo("Starving", 0, -2, 0, 0, 0, 0),
        new ConditionInfo("Near-death", 0, 0, 0, 0, 0, 0),
    };

    /// <summary>The constants for one affliction.</summary>
    public static ConditionInfo Info(ActorCondition condition) {
        int index = (int)condition;
        if (index < 0 || index >= ActorConditions.Count) {
            throw new ArgumentOutOfRangeException(nameof(condition));
        }
        return Table[index];
    }

    /// <summary>What applying a delta did, for the caller that owns the party-wide bookkeeping.</summary>
    public readonly struct ConditionChange {
        public ConditionChange(int rank, bool appeared, bool cleared, bool raisesEvent,
            bool collapsed) {
            Rank = rank;
            Appeared = appeared;
            Cleared = cleared;
            RaisesEvent = raisesEvent;
            Collapsed = collapsed;
        }

        /// <summary>The rank afterwards.</summary>
        public int Rank { get; }

        /// <summary>Did the actor pick this up (0 → non-zero)?</summary>
        public bool Appeared { get; }

        /// <summary>Did it just go away (non-zero → 0)?</summary>
        public bool Cleared { get; }

        /// <summary>
        /// Should the actor's CONDITION global be written? False for Drunk and Healing, which the
        /// original never announces, and for Near-death during combat.
        /// </summary>
        public bool RaisesEvent { get; }

        /// <summary>
        /// The actor was just driven further into Near-death, which wipes every other affliction
        /// and resets the health pool. See <see cref="Apply"/>.
        /// </summary>
        public bool Collapsed { get; }
    }

    /// <summary>
    /// Change one affliction's rank by <paramref name="amount"/>, clamped to 0..100.
    ///
    /// <para><b>Near-death is not just another rank.</b> Pushing it up wipes all six other
    /// afflictions, empties Health and Stamina, and then refills the pool — but because the
    /// near-death rank is now set, the refill caps at a sliver that shrinks as the rank rises
    /// (<see cref="StatEngine.ModifyHealthPool"/>'s near-death branch). That is the engine's
    /// "knocked out but alive" state, and it is why a collapsed character comes back with a trickle
    /// of health and nothing else wrong with them. Pass <paramref name="health"/> and
    /// <paramref name="stamina"/> to have that happen; omit them and only the ranks change.</para>
    ///
    /// <para><paramref name="inCombat"/> only suppresses the Near-death event, matching
    /// <c>g_wInCombatMode</c>.</para>
    /// </summary>
    public static ConditionChange Apply(ActorConditions conditions, ActorCondition condition,
        int amount, ActorStat health = null, ActorStat stamina = null, bool inCombat = false) {
        if (conditions == null) {
            throw new ArgumentNullException(nameof(conditions));
        }
        int index = (int)condition;
        if (index < 0 || index >= ActorConditions.Count) {
            throw new ArgumentOutOfRangeException(nameof(condition));
        }
        if (amount == 0) {
            return new ConditionChange(conditions[condition], false, false, false, false);
        }

        int before = conditions[condition];
        conditions[condition] = before + amount;
        int after = conditions[condition];

        bool appeared = before == 0 && after != 0;
        bool cleared = before != 0 && after == 0;

        // Drunk and Healing are never announced; Near-death is not announced mid-combat.
        bool announceable = condition != ActorCondition.Healing
            && condition != ActorCondition.Drunk
            && (condition != ActorCondition.NearDeath || !inCombat);
        bool raisesEvent = announceable && (appeared || cleared);

        bool collapsed = false;
        if (condition == ActorCondition.NearDeath && amount > 0) {
            collapsed = true;
            for (int i = 0; i < ActorConditions.Count; i++) {
                if (i != (int)ActorCondition.NearDeath) {
                    conditions[(ActorCondition)i] = 0;
                }
            }
            // The 1.00 floppy build also wrote a clear-event for each of those six; the 1.02 CD
            // build we target does not (#ifndef V102CD), so nothing is announced here.
            if (health != null && stamina != null) {
                health.Base = 0;
                stamina.Base = 0;
                StatEngine.ModifyHealthPool(health, stamina, 0x7fff, healTargetPercent: 100,
                    out _, nearDeathRank: after);
            }
        }

        return new ConditionChange(after, appeared, cleared, raisesEvent, collapsed);
    }

    /// <summary>
    /// The read-time penalty an actor's afflictions impose on one attribute — the step
    /// <c>stat_actor_get</c> runs between the equipment modifier and the health scaling. Pass this
    /// as <see cref="StatEngine.Get"/>'s effects hook.
    ///
    /// <para>Only Drunk actually penalises anything today, and it hits Stamina, Defence, all three
    /// accuracies and every craft skill — but not Health, Speed or Strength. At rank 100 those drop
    /// to 40% of normal, scaling linearly from no effect at rank 0.</para>
    /// </summary>
    public static int ApplyAttributePenalties(int value, ActorAttribute attribute,
        ActorConditions conditions) {
        if (conditions == null) {
            return value;
        }
        int statIndex = (int)attribute;
        for (int i = 0; i < ActorConditions.Count; i++) {
            int rank = conditions[(ActorCondition)i];
            if (rank <= 0) {
                continue;
            }
            ConditionInfo info = Table[i];
            if (AppliesTo(info.FirstMask, statIndex)) {
                value = value * (info.FirstPercent * rank / 100 + 100) / 100;
            }
            if (AppliesTo(info.SecondMask, statIndex)) {
                value = value * (info.SecondPercent * rank / 100 + 100) / 100;
            }
        }
        return value;
    }

    /// <summary>
    /// Does an affliction's attribute mask cover this attribute?
    ///
    /// <para>The masks are stored as negative shorts and the original tests them as
    /// <c>mask &amp; (1 &lt;&lt; index)</c> after integer promotion — which <b>sign-extends</b>, so
    /// every bit above 15 is set too. For Drunk's -14 (…11110010) that means attributes 1, 4, 5, 6,
    /// 7 and everything from 8 up are affected, while 0, 2 and 3 are not. Reproduced rather than
    /// tidied: the sign extension is what the shipped game does.</para>
    /// </summary>
    private static bool AppliesTo(int mask, int statIndex) =>
        mask != 0 && (mask & (1 << statIndex)) != 0;

    /// <summary>
    /// How much an affliction's own rank moves in one hour, including the fact that <b>being under
    /// Healing actively cures the others</b>: it pulls the three diseases and Drunk down by a
    /// further 2 (3 for Sick) on top of their own drift, which is what turns Sick's natural +1 per
    /// hour into a -2. The hourly upkeep pass applies this; the table alone does not tell the
    /// whole story.
    /// </summary>
    public static int HourlyDelta(ActorCondition condition, ActorConditions conditions) {
        int index = (int)condition;
        int amount = Table[index].HourlyDelta;
        if (index < 4 && conditions != null && conditions.Has(ActorCondition.Healing)) {
            amount -= (index == 0 ? 1 : 0) + 2;
        }
        return amount;
    }

    /// <summary>
    /// What the actor's current afflictions add to their hourly health regeneration — negative for
    /// anything that is wearing them down, positive while Healing.
    /// </summary>
    public static int RegenBonus(ActorConditions conditions) {
        if (conditions == null) {
            return 0;
        }
        int total = 0;
        for (int i = 0; i < ActorConditions.Count; i++) {
            if (conditions[(ActorCondition)i] != 0) {
                total += Table[i].RegenDelta;
            }
        }
        return total;
    }
}
