namespace GameData.Resources.Combat;

/// <summary>
/// The action-priority tables for the two non-casting monster AI turns —
/// <c>monster_chooseCrossbowAction</c> (ovr172 @0x6660c) and
/// <c>monster_chooseMeleeMoveAction</c> (ovr170 @0x65652).
///
/// <para>Sibling to <see cref="MonsterSpellcasting"/>, which is the same mechanism for the caster
/// branch. The cascade picks ONE of the three branches — caster, else crossbow, else melee/move —
/// and each branch then walks its own priority row, trying action slots until one commits.</para>
/// </summary>
/// <remarks>
/// <b>Both tables are 1-BASED, and the base symbol sits one row BEFORE row 1.</b> Indexing is
/// <c>pattern &lt;&lt; 4 + attempt * 2</c> bytes from the base, so row 0 would decode the tail of
/// the preceding array as slot numbers. The code guarantees it is never read — see
/// <see cref="Shoots"/> — which is the only reason the overlap is safe. Reading either table as
/// 0-based gives every monster the wrong action order.
/// </remarks>
public static class MonsterActionPatterns {
    /// <summary>Action slots in a row. Both families use eight, like the caster's.</summary>
    public const int SlotCount = 8;

    /// <summary>Highest pattern either table defines. Pattern 0 has no row.</summary>
    public const int MaxPattern = 8;

    /// <summary><c>crossbowPattern_actionPriority</c> @0x3B428, rows 1-8.</summary>
    private static readonly int[][] CrossbowPriority = {
        new[] { 1, 4, 6, 5, 7, 8, 3, 2 },
        new[] { 2, 8, 3, 7, 4, 5, 6, 1 },
        new[] { 3, 6, 7, 8, 2, 1, 4, 5 },
        new[] { 4, 2, 7, 3, 8, 1, 5, 6 },
        new[] { 5, 8, 4, 1, 6, 7, 2, 3 },
        new[] { 6, 3, 8, 7, 5, 4, 2, 1 },
        new[] { 7, 3, 2, 8, 6, 1, 5, 4 },
        new[] { 8, 6, 7, 3, 4, 2, 1, 5 },
    };

    /// <summary><c>meleeMovePattern_actionPriority</c> @0x3B2E8, rows 1-8.</summary>
    private static readonly int[][] MeleeMovePriority = {
        new[] { 1, 8, 4, 6, 3, 2, 7, 5 },
        new[] { 2, 4, 8, 5, 3, 7, 6, 1 },
        new[] { 3, 6, 7, 8, 1, 2, 5, 4 },
        new[] { 4, 5, 8, 6, 7, 2, 3, 1 },
        new[] { 5, 4, 3, 7, 2, 6, 1, 8 },
        new[] { 6, 1, 2, 3, 7, 8, 4, 5 },
        new[] { 7, 5, 8, 4, 1, 2, 6, 3 },
        new[] { 8, 2, 4, 5, 3, 6, 7, 1 },
    };

    /// <summary>
    /// The attempt index each branch starts from — <b>and they differ</b>.
    /// </summary>
    /// <remarks>
    /// <c>monster_chooseMeleeMoveAction</c> opens <c>xor di, di</c>, so it uses its row from slot 0.
    /// <c>monster_chooseCrossbowAction</c> opens <c>mov di, 1</c> and therefore <b>never reads the
    /// first entry of its own row</b>. Since every row begins with its own pattern number, the
    /// crossbow turn is skipping that self-referential entry and starting at the real fallback
    /// order — which is easy to miss and changes the action order for every shooter in the game.
    /// </remarks>
    public const int CrossbowFirstAttempt = 1;

    /// <inheritdoc cref="CrossbowFirstAttempt"/>
    public const int MeleeMoveFirstAttempt = 0;

    /// <summary>
    /// The crossbow turn's per-attempt commit roll: it acts while <c>rnd % 100</c> is under this.
    /// </summary>
    /// <remarks>
    /// <c>cmp var_2, 91 / jge</c> skips the attempt (and still advances the counter), so 91 is the
    /// chance to try the slot at all, not to hit with it. The melee/move turn has <b>no</b>
    /// equivalent roll — it walks its row straight through.
    /// </remarks>
    public const int CrossbowCommitPercent = 91;

    /// <summary>
    /// <b>Pattern 0 means the creature never takes that branch's actions.</b>
    /// </summary>
    /// <remarks>
    /// In both routines the test sits before the <i>first</i> attempt as well as between them
    /// (<c>cmp [combatData.crossbowPattern], 0 / jnz</c> at the loop head), so a pattern of 0
    /// produces no action at all and the turn falls through to the fatigue/morale fallback — rest,
    /// or advance on the chosen target.
    /// </remarks>
    public static bool Shoots(int crossbowPattern) =>
        crossbowPattern > 0 && crossbowPattern <= MaxPattern;

    /// <inheritdoc cref="Shoots"/>
    public static bool Fights(int meleeMovePattern) =>
        meleeMovePattern > 0 && meleeMovePattern <= MaxPattern;

    /// <summary>What a row slot does once its number is looked up.</summary>
    public enum SlotKind {
        None,
        /// <summary><c>combataipath_follow_tgt_check(actor, Radius, Role)</c>: keep an orthogonally
        /// adjacent target, else choose one by role and close on it.</summary>
        Follow,
        /// <summary><c>combataiturn_action_disp_base(actor, Radius, Role)</c>: choose by role, shoot down a
        /// clear line of fire.</summary>
        Shot,
        /// <summary><c>combataiturn_select_and_engage</c>: raise a guard, else the (Radius, Role) shot.</summary>
        Engage,
        /// <summary><c>combataipath_low_health_action</c>: rest when worn and nobody is close.</summary>
        RestWhenWorn,
    }

    /// <summary>One decoded slot.</summary>
    public readonly struct Slot {
        public readonly SlotKind Kind;
        public readonly int Radius;
        public readonly TargetRole Role;

        public Slot(SlotKind kind, int radius = 0, TargetRole role = TargetRole.Anyone) {
            Kind = kind;
            Radius = radius;
            Role = role;
        }
    }

    /// <summary><c>g_encounter_ai_action_table</c> (CBTAITRN.C:18), slots 1-8.</summary>
    /// <remarks>Six of the eight are one shot routine with a different (radius, mode) pair; mode is the
    /// <see cref="TargetRole"/> number. Note the 4000a/5000a/3000a names are not in mode order.</remarks>
    private static readonly Slot[] CrossbowSlots = {
        new Slot(SlotKind.Follow, 6, TargetRole.Anyone),              // 1 combataipath_action_6
        new Slot(SlotKind.Shot, 6, TargetRole.Anyone),                // 2 action_kind6
        new Slot(SlotKind.Shot, 10, TargetRole.Spellcaster),          // 3 action_1000a
        new Slot(SlotKind.Shot, 10, TargetRole.Wounded),              // 4 action_2000a
        new Slot(SlotKind.Shot, 10, TargetRole.Engaged),              // 5 action_4000a
        new Slot(SlotKind.Shot, 10, TargetRole.TargetingTheLeader),   // 6 action_5000a
        new Slot(SlotKind.Shot, 10, TargetRole.MissileCapable),       // 7 action_3000a
        new Slot(SlotKind.Engage, 10, TargetRole.Engaged),            // 8 select_and_engage → action_4000a
    };

    /// <summary><c>g_combat_ai_action_table</c> (CMBTAI.C), slots 1-8.</summary>
    private static readonly Slot[] MeleeMoveSlots = {
        new Slot(SlotKind.RestWhenWorn),                              // 1 low_health_action
        new Slot(SlotKind.Follow, 6, TargetRole.Anyone),              // 2 action_6
        new Slot(SlotKind.Follow, 100, TargetRole.Spellcaster),       // 3 action_100_1
        new Slot(SlotKind.Follow, 100, TargetRole.Wounded),           // 4 action_100_2
        new Slot(SlotKind.Follow, 100, TargetRole.Disengaged),        // 5 action_60064
        new Slot(SlotKind.Follow, 100, TargetRole.TargetingTheLeader),// 6 action_50064
        new Slot(SlotKind.Follow, 100, TargetRole.MissileCapable),    // 7 action_30064
        new Slot(SlotKind.Follow, 100, TargetRole.Engaged),           // 8 action_40064
    };

    /// <summary>What crossbow slot 1-8 does; <see cref="SlotKind.None"/> out of range.</summary>
    public static Slot CrossbowSlot(int slot) =>
        slot >= 1 && slot <= SlotCount ? CrossbowSlots[slot - 1] : default;

    /// <summary>What melee/move slot 1-8 does; <see cref="SlotKind.None"/> out of range.</summary>
    public static Slot MeleeMoveSlot(int slot) =>
        slot >= 1 && slot <= SlotCount ? MeleeMoveSlots[slot - 1] : default;

    /// <summary><c>combataiturn_take_actor_turn</c>: under this much health no crossbow attempt is tried.</summary>
    public const int CrossbowAttemptsMinHealth = 5;

    /// <summary>The crossbow turn's fallback when nothing acted: advance on a roll under 75, or always at
    /// full stamina; otherwise rest (CBTAITRN.C:361-367).</summary>
    public static bool CrossbowFallbackAdvances(int roll100, int staminaPercent) =>
        roll100 < 75 || staminaPercent == 100;

    /// <summary>The caster turn's fallback when no slot acted (<c>combat_ai_take_turn</c>, CBTAI.C:373-381):
    /// a creature above this share of its pool advances, on a roll over <see cref="CasterAdvanceRollAbove"/>;
    /// anything else, and every party member, raises its guard.</summary>
    public const int CasterAdvancePoolPercent = 40;

    /// <inheritdoc cref="CasterAdvancePoolPercent"/>
    public const int CasterAdvanceRollAbove = 10;

    /// <summary>The caster's advance is capped at its nearest opponent's distance less six, never under
    /// one step (CBTAI.C:376-379): a caster closes slowly and stays at range.</summary>
    public static int CasterAdvanceSteps(int speed, int nearestDistance) =>
        System.Math.Min(speed, System.Math.Max(nearestDistance, 7) - 6);

    /// <summary><c>combataipath_low_health_action</c> (CMBTAI.C:437): under 75% stamina, nobody within
    /// two, and a roll under 80.</summary>
    public static bool LowHealthRests(int staminaPercent, int nearestDistance, int roll100) =>
        staminaPercent < 75 && nearestDistance > 2 && roll100 < 80;

    /// <summary>The melee/move fallback's exhaustion test: under this stamina percentage…</summary>
    public const int ExhaustedStaminaPercent = 10;

    /// <summary>…a roll under this rests…</summary>
    public const int ExhaustedRestPercent = 25;

    /// <summary>…and a second roll at or under this backs off first when engaged (CMBTAI.C:507-514).</summary>
    public const int ExhaustedBackOffPercent = 20;

    /// <summary><c>select_and_engage</c>: anyone within (speed − this) is in reach.</summary>
    public const int EngageReachMargin = 3;

    /// <summary><c>select_and_engage</c>: a roll at or over this raises a guard against someone in reach.</summary>
    public const int EngageNearGuardPercent = 50;

    /// <summary><c>select_and_engage</c>: a roll at or over this raises a guard against a spellcaster in
    /// the line of fire.</summary>
    public const int EngageFarGuardPercent = 75;

    /// <summary>The action slot the crossbow turn tries on the given attempt.</summary>
    /// <returns>A slot in 1-8, or 0 when the pattern never shoots or the attempt is out of range.</returns>
    public static int CrossbowSlotFor(int crossbowPattern, int attempt) =>
        !Shoots(crossbowPattern) || attempt < 0 || attempt >= SlotCount
            ? 0
            : CrossbowPriority[crossbowPattern - 1][attempt];

    /// <summary>The action slot the melee/move turn tries on the given attempt.</summary>
    /// <returns>A slot in 1-8, or 0 when the pattern never acts or the attempt is out of range.</returns>
    public static int MeleeMoveSlotFor(int meleeMovePattern, int attempt) =>
        !Fights(meleeMovePattern) || attempt < 0 || attempt >= SlotCount
            ? 0
            : MeleeMovePriority[meleeMovePattern - 1][attempt];
}
