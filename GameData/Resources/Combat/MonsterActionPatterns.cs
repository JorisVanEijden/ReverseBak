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
