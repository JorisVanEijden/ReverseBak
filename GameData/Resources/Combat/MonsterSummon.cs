namespace GameData.Resources.Combat;

/// <summary>
/// Putting a conjured creature on the combat grid — <c>combat_summon_creature</c> (ovr173 @0x67655,
/// named this session; it was <c>sub_ovr173_F55</c>).
/// </summary>
/// <remarks>
/// Reached from <c>Cast_Spell</c>'s <b>targeting-type</b> case 6, with the spell record's polymorphic
/// <c>Color</c> field carrying the creature type. So "which creature does this spell summon" is not a
/// field of its own — it shares the slot that means a colour for other spells.
/// </remarks>
public static class MonsterSummon {
    /// <summary>
    /// <b>A summon can simply fail, and the spell is spent anyway.</b>
    /// </summary>
    /// <remarks>
    /// The roster add is the first thing tried; a full field shows <see cref="NoRoomDialog"/> and the
    /// routine gives up before anything else happens. Nothing refunds the cast — a port that checks
    /// for room before charging is being kinder than the game.
    /// </remarks>
    public static bool Succeeds(bool rosterHadRoom) => rosterHadRoom;

    /// <summary>Shown when there is no room on the roster.</summary>
    public const int NoRoomDialog = 145;

    /// <summary>The cue a summon plays — the same creation sound the lighting spells use.</summary>
    public const int Sound = 0x3a;

    /// <summary>
    /// <b>A summoned creature knows NO spells, whatever its type normally casts.</b>
    /// </summary>
    /// <remarks>
    /// All three spell words are zeroed at spawn. So conjuring a creature whose kind is a caster
    /// gets you its body and not its book — a port that copies the template's spell lists produces a
    /// summon far stronger than the game's.
    /// </remarks>
    public static bool KnowsSpells => false;

    /// <summary>
    /// The flee threshold a summon is given: <b>zero, and NOT the never-flees sentinel.</b>
    /// </summary>
    /// <remarks>
    /// The field is ZEROED at spawn rather than taken from the creature's template, so a conjured
    /// creature does not inherit its kind's nerve.
    ///
    /// <para><b>Do not read zero as fearless.</b> <see cref="MonsterMorale.NeverFleesMorale"/> is
    /// 0xff, so zero is the opposite end of the scale — what it actually does is
    /// <see cref="MonsterMorale"/>'s business, and this class only records that the template value
    /// is discarded. I first wrote this up as "it never routs"; the sentinel says otherwise.</para>
    /// </remarks>
    public const int FleeThreshold = 0;

    /// <summary>The movement pattern a summon is given, on both disciplines.</summary>
    /// <remarks>
    /// <b>Written, not read.</b> Both <c>meleeMovePattern</c> and <c>crossbowPattern</c> are set to
    /// this at spawn — the routine assigns the defaults rather than consulting them, which is what
    /// made it look like a pattern dispatcher from the outside.
    /// </remarks>
    public const int Pattern = 1;

    /// <summary>The status word a summon starts in.</summary>
    public const int InitialStatus = 128;

    /// <summary>No spell effect is attached at spawn.</summary>
    public const int NoEffectSlot = -1;

    /// <summary>
    /// Whether the caller is asked to pick the tile.
    /// </summary>
    /// <remarks>
    /// <b>A spell-cast summon does NOT prompt.</b> The routine takes a flag, and <c>Cast_Spell</c>
    /// passes zero — the creature lands on the position already in the placement globals. Only the
    /// other caller asks. Worth knowing before building a tile-picker into the spell path.
    /// </remarks>
    public static bool PromptsForTile(bool promptFlag) => promptFlag;
}
