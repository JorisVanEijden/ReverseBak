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
    /// The morale a summon is given: <b>zero, which means it never routs.</b>
    /// </summary>
    /// <remarks>
    /// <b>An earlier version of this remark said the opposite</b> — that zero must not be read as
    /// fearless, because <see cref="MonsterMorale.NeverFleesMorale"/> (0xff) is the sentinel and
    /// zero is "the other end of the scale". That was wrong, and it contradicted two models in this
    /// same folder. There are TWO never-flee values, tested at different points:
    /// <see cref="MonsterMorale.Routs"/> rejects 0xff before it computes anything and rejects zero
    /// after the roll has been made and passed. <see cref="Monster.MonsterStats.FleeThreshold"/>
    /// says the same thing — 0 means never, and MONST19/MONST28 ship it. So does
    /// <see cref="MonsterFleeDestination.WontMoveMorale"/>, corrected against IDA the day before
    /// this was: THREE models in this folder agreed and only this one dissented.
    ///
    /// <para><b>The zero is deliberate and it STICKS, which is the part that proves it.</b> The
    /// stat roll that follows reads the template's morale only <c>if (morale != 0)</c>, so zeroing
    /// the field first is precisely what stops the creature's own nerve being applied. Compare
    /// <see cref="Pattern"/>, which the same roll overwrites unconditionally: one of those two
    /// assignments survives and the other does not, and the guard is the difference.</para>
    /// </remarks>
    public const int Morale = 0;

    /// <summary><b>A summoned creature never routs.</b></summary>
    /// <inheritdoc cref="Morale"/>
    public static bool Routs => false;

    /// <summary>
    /// The AI profile a summon is assigned at spawn — <b>and then does not keep.</b>
    /// </summary>
    /// <remarks>
    /// <b>This value never survives the function that writes it.</b> The routine sets both AI
    /// profile bytes to 1 and then calls the MONSTXX.DAT stat roll three lines later, which reads
    /// the creature's own profile fields over the top of them unconditionally — no guard, unlike
    /// <see cref="Morale"/>. So a summoned creature fights on ITS KIND'S AI profiles, exactly like
    /// any other instance of that creature, and a port that pins every summon to profile 1 makes
    /// them all behave identically.
    ///
    /// <para>The constant is kept rather than deleted because the assignment is really there, and
    /// because this is the field that made the routine look like a pattern dispatcher from the
    /// outside — it WRITES the two bytes an earlier reading had it consulting. The bytes are the
    /// runtime combatant's AI profiles, which are fed from
    /// <see cref="Monster.MonsterStats.CrossbowPattern"/> and
    /// <see cref="Monster.MonsterStats.MeleeMovePattern"/>; describing them as a "movement pattern
    /// on both disciplines", as this did, conflates the runtime fields with the file's.</para>
    /// </remarks>
    public const int Pattern = 1;

    /// <summary><b>Overwritten by the stat roll, so nothing downstream sees it.</b></summary>
    /// <inheritdoc cref="Pattern"/>
    public static bool PatternSurvivesTheStatRoll => false;

    /// <summary>
    /// The flags a summon starts with: <see cref="CombatantFlags.AiSummon"/> and nothing else.
    /// </summary>
    /// <remarks>
    /// <b>It is the flags word, not a status word, and the distinction has teeth.</b> The routine
    /// ASSIGNS 0x80 rather than OR-ing it, so <see cref="CombatantFlags.Ready"/> is clear — a
    /// conjured creature does not act on the round it lands, and only the next round reset gives it
    /// a turn. Setting the summon bit into an otherwise ready combatant would hand the caster a free
    /// extra action the moment the spell resolves.
    /// </remarks>
    public const CombatantFlags InitialFlags = CombatantFlags.AiSummon;

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
