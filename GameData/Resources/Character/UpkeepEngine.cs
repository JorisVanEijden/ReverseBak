namespace GameData.Resources.Character;

using GameData.Resources.Inventory;
using GameData.Resources.Object;
using System;

/// <summary>
/// What the passage of time does to a party member: hourly regeneration and affliction drift,
/// exhaustion when they go too long without rest, and the once-a-day recoveries. Ported from
/// <c>gstate_advance_time</c> (0x42b37) and its hourly tick <c>gstate_hourly_tick</c> (0x4291e),
/// canassa <c>SRC/GAME/GSTATE.C</c>.
///
/// <para>These are the per-actor rules only. Deciding <i>when</i> to run them (the clock's hour and
/// day boundaries), which actors are in the party, and what to do with the results — the tiredness
/// dialog, the improvement flags — belongs to the caller.</para>
/// </summary>
public static class UpkeepEngine {
    /// <summary>
    /// Awake this long without resting and the party is warned they are tired (0x7788 ticks = 17
    /// hours).
    /// </summary>
    public const long ExhaustionWarningTicks = 0x7788;

    /// <summary>
    /// Awake past this and they start losing health every hour instead of just being warned
    /// (0x7e90 ticks = 18 hours).
    /// </summary>
    public const long ExhaustionDrainTicks = 0x7e90;

    /// <summary>Health and Stamina maximums creep up once every this many days.</summary>
    public const int GrowthIntervalDays = 30;

    /// <summary>Neither maximum grows past this.</summary>
    public const int GrowthCeiling = 0xfa;

    /// <summary>
    /// Health lost per hour, per character, once past <see cref="ExhaustionDrainTicks"/>
    /// (<c>g_abSleepStatDelta</c>). Indexed by character id — Gorath (index 5) tires fastest and
    /// the second character slowest.
    /// </summary>
    public static readonly sbyte[] ExhaustionDrainPerCharacter = { -2, -1, -2, -2, -2, -3 };

    /// <summary>
    /// Health regained per hour of rest, per character (<c>g_abRegenPerChar</c>). Flat 1 for
    /// everyone in the shipped data — the per-character table exists but was never varied.
    /// </summary>
    public static readonly byte[] RegenerationPerCharacter = { 1, 1, 1, 1, 1, 1 };

    /// <summary>
    /// A rest that heals only to 80% of the pool rather than filling it. The engine picks the
    /// lower cap for exactly this value of the rest argument.
    /// </summary>
    public const int PartialRestQuality = 100;

    private const int PartialRestCapPercent = 0x50;
    private const int FullRestCapPercent = 100;

    /// <summary>
    /// One hour of upkeep for one actor.
    ///
    /// <para><b>Regeneration only happens while resting.</b> <paramref name="restQuality"/> is 0
    /// when the party is walking around — the world loop passes zero — and no health comes back at
    /// all. Rest at <see cref="PartialRestQuality"/> tops the pool up to 80%; any other non-zero
    /// value fills it. Resting also knocks 3 off Sick on top of its own drift.</para>
    ///
    /// <para>Afflictions drift every hour either way (see
    /// <see cref="ConditionEngine.HourlyDelta"/>) and their regeneration penalties are folded into
    /// the amount healed, so a poisoned character rests at a loss.</para>
    /// </summary>
    /// <param name="characterIndex">Character id, indexing the per-character tables.</param>
    /// <param name="restQuality">0 = not resting; <see cref="PartialRestQuality"/> = rest to 80%;
    /// any other non-zero = rest to full.</param>
    public static void ApplyHour(ActorStat health, ActorStat stamina, ActorConditions conditions,
        int characterIndex, int restQuality) {
        if (health == null) {
            throw new ArgumentNullException(nameof(health));
        }
        if (stamina == null) {
            throw new ArgumentNullException(nameof(stamina));
        }
        if (conditions == null) {
            throw new ArgumentNullException(nameof(conditions));
        }

        int regeneration = 0;
        int capPercent = FullRestCapPercent;

        if (restQuality != 0) {
            // Rest is itself a treatment for being sick.
            ConditionEngine.Apply(conditions, ActorCondition.Sick, -3, health, stamina);

            capPercent = restQuality == PartialRestQuality ? PartialRestCapPercent : FullRestCapPercent;
            regeneration = RegenerationFor(characterIndex) * restQuality / 100;
            if (conditions.Has(ActorCondition.Healing)) {
                regeneration *= 2;
            }
        }

        // Every affliction drifts, and the ones that sap you drag the hour's healing down with them.
        for (int i = 0; i < ActorConditions.Count; i++) {
            var condition = (ActorCondition)i;
            if (!conditions.Has(condition)) {
                continue;
            }
            ConditionEngine.Apply(conditions, condition,
                ConditionEngine.HourlyDelta(condition, conditions), health, stamina);
        }
        regeneration += ConditionEngine.RegenBonus(conditions);

        if (regeneration != 0) {
            StatEngine.ModifyHealthPool(health, stamina, regeneration * 0x100, capPercent, out _);
        }
    }

    /// <summary>Good rations — a day's food.</summary>
    public const int RationsObjectId = 72;

    /// <summary>Poisoned rations. Eating these does NOT stop the hunger.</summary>
    public const int PoisonedRationsObjectId = 73;

    /// <summary>Spoiled rations — they feed you, at a cost.</summary>
    public const int SpoiledRationsObjectId = 74;

    /// <summary>
    /// A day's meal for one party member — <c>gstate_member_consume_rations</c>.
    ///
    /// <para>The party eats in strict order of preference and stops at the first thing it finds:
    /// good rations, then spoiled (which feed you but add 3 to Sick), then poisoned. Poisoned
    /// rations are the cruel case — they add 4 to Poisoned and, unlike the other two, do
    /// <b>not</b> clear Starving, so a party down to poisoned rations gets poisoned and stays
    /// hungry. With nothing at all to eat, Starving climbs by 5.</para>
    /// </summary>
    /// <returns>What the member ate, or <see cref="Meal.WentHungry"/>.</returns>
    public static Meal ConsumeRations(RuntimeContainer inventory, ActorConditions conditions,
        Func<int, ObjectInfo> lookup) {
        if (conditions == null) {
            throw new ArgumentNullException(nameof(conditions));
        }

        if (inventory != null && InventoryConsume.TryConsumeOne(inventory, RationsObjectId, lookup)) {
            ConditionEngine.Apply(conditions, ActorCondition.Starving, -100);
            return Meal.Rations;
        }
        if (inventory != null && InventoryConsume.TryConsumeOne(inventory, SpoiledRationsObjectId, lookup)) {
            ConditionEngine.Apply(conditions, ActorCondition.Starving, -100);
            ConditionEngine.Apply(conditions, ActorCondition.Sick, 3);
            return Meal.SpoiledRations;
        }
        if (inventory != null && InventoryConsume.TryConsumeOne(inventory, PoisonedRationsObjectId, lookup)) {
            ConditionEngine.Apply(conditions, ActorCondition.Poisoned, 4);
            return Meal.PoisonedRations;
        }

        ConditionEngine.Apply(conditions, ActorCondition.Starving, 5);
        return Meal.WentHungry;
    }

    /// <summary>
    /// The hourly cost of staying awake too long, applied once the party has gone
    /// <see cref="ExhaustionDrainTicks"/> without resting. Returns false when this actor has been
    /// worn down to nothing — the original uses that to decide whether the whole party is still on
    /// its feet before nagging them to sleep.
    /// </summary>
    public static bool ApplyExhaustion(ActorStat health, ActorStat stamina, int characterIndex) {
        if (health == null) {
            throw new ArgumentNullException(nameof(health));
        }
        if (stamina == null) {
            throw new ArgumentNullException(nameof(stamina));
        }
        int drain = ExhaustionDrainFor(characterIndex);
        int remaining = StatEngine.ModifyHealthPool(health, stamina, drain * 0x100,
            FullRestCapPercent, out _);
        return remaining != 0;
    }

    /// <summary>
    /// Whether time awake has reached the point of a warning, of actual harm, or neither.
    /// </summary>
    public static ExhaustionLevel ExhaustionAfter(long ticksSinceRest) {
        if (ticksSinceRest >= ExhaustionDrainTicks) {
            return ExhaustionLevel.Draining;
        }
        return ticksSinceRest >= ExhaustionWarningTicks ? ExhaustionLevel.Tired : ExhaustionLevel.Rested;
    }

    /// <summary>
    /// A day's worth of climbing out of Near-death. The rank falls by
    /// <c>(rank - 100) / 10 - 1</c> — so the closer to death, the slower the crawl back — and twice
    /// that while under Healing. Does nothing to an actor who is not near death.
    /// </summary>
    public static void ApplyDailyNearDeathRecovery(ActorConditions conditions) {
        if (conditions == null) {
            throw new ArgumentNullException(nameof(conditions));
        }
        int rank = conditions[ActorCondition.NearDeath];
        if (rank == 0) {
            return;
        }
        int recovery = (rank - 100) / 10 - 1;
        if (conditions.Has(ActorCondition.Healing)) {
            recovery *= 2;
        }
        ConditionEngine.Apply(conditions, ActorCondition.NearDeath, recovery);
    }

    /// <summary>
    /// The slow constitutional gain: every <see cref="GrowthIntervalDays"/> days each party member's
    /// Health and Stamina <i>maximums</i> go up by one, to a ceiling of
    /// <see cref="GrowthCeiling"/>. Returns true when anything actually grew, which is the
    /// character's "improved" signal.
    /// </summary>
    public static bool ApplyPeriodicGrowth(ActorStat health, ActorStat stamina) {
        if (health == null) {
            throw new ArgumentNullException(nameof(health));
        }
        if (stamina == null) {
            throw new ArgumentNullException(nameof(stamina));
        }
        bool grew = false;
        if (health.Max < GrowthCeiling) {
            health.Max++;
            grew = true;
        }
        if (stamina.Max < GrowthCeiling) {
            stamina.Max++;
            grew = true;
        }
        return grew;
    }

    /// <summary>
    /// Is <paramref name="dayIndex"/> one of the days the maximums grow on? The original tests the
    /// day the party is leaving, not the one they are entering.
    /// </summary>
    public static bool IsGrowthDay(long dayIndex) => dayIndex % GrowthIntervalDays == 0;

    private static int RegenerationFor(int characterIndex) =>
        characterIndex >= 0 && characterIndex < RegenerationPerCharacter.Length
            ? RegenerationPerCharacter[characterIndex]
            : RegenerationPerCharacter[0];

    private static int ExhaustionDrainFor(int characterIndex) =>
        characterIndex >= 0 && characterIndex < ExhaustionDrainPerCharacter.Length
            ? ExhaustionDrainPerCharacter[characterIndex]
            : ExhaustionDrainPerCharacter[0];
}

/// <summary>What a party member managed to eat on a given day.</summary>
public enum Meal {
    /// <summary>Nothing to eat — Starving climbs.</summary>
    WentHungry,

    /// <summary>A proper day's rations.</summary>
    Rations,

    /// <summary>Spoiled rations: fed, but sickened.</summary>
    SpoiledRations,

    /// <summary>Poisoned rations: poisoned, and still hungry.</summary>
    PoisonedRations,
}

/// <summary>How far past its last rest the party is.</summary>
public enum ExhaustionLevel {
    /// <summary>No effect yet.</summary>
    Rested,

    /// <summary>Long enough that the party is told they are tired.</summary>
    Tired,

    /// <summary>Long enough that staying up is costing them health every hour.</summary>
    Draining,
}
