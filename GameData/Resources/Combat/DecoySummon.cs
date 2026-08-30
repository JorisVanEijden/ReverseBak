namespace GameData.Resources.Combat;

using GameData.Resources.Spells;

/// <summary>
/// Dannon's Delusions — a decoy copy of the caster, put on the grid
/// (<c>combat_summon_decoy</c>, ovr173 @0x676f7, named this session).
/// </summary>
/// <remarks>
/// <b>Not the same thing as <see cref="MonsterSummon"/>, though both put an actor on the grid.</b>
/// That one conjures a real creature of a named type and takes no effect slot; this one copies the
/// CASTER, is helpless by construction, and expires with its spell. Folding them into one "summon"
/// would give the decoy a monster's body or the monster an expiry.
/// </remarks>
/// <remarks>
/// <b>AWAITING ITS FEATURE (TASK-112).</b> Dannon's Delusions puts no actor on the grid yet — the spell resolves and the decoy is
/// the part that is missing.
///
/// <para>Read by <c>scripts/audit-unconsumed-models.py</c>: it separates a rule ported ahead
/// of its feature from an orphan nobody owns, so the audit stays a signal instead of a list
/// to re-triage every run.</para>
/// </remarks>
public static class DecoySummon {
    /// <summary>The spell the decoy's effect slot is stamped with.</summary>
    /// <remarks>
    /// <b>The routine writes a literal 1 into the slot's type field, and that field holds a spell
    /// id</b> — <c>cspell_status_effect_add</c> puts spell numbers there, 0xd for Grief of a
    /// Thousand Nights among them. So the 1 IS Dannon's Delusions rather than an effect-kind that
    /// happens to be 1; worth stating, because the two readings are indistinguishable at the call
    /// site and only one of them is right.
    ///
    /// <para>It also explains the spell: Dannon's Delusions is one of the two whose computed
    /// magnitude the post-animation hook throws away
    /// (<see cref="Spells.SpellCastTail.ZeroesItsOwnMagnitude"/>). It carries a CostTimesDamage
    /// calculation, runs the arithmetic, discards the answer — and this decoy is the entire
    /// effect.</para>
    /// </remarks>
    public const int Spell = SpellIds.DannonsDelusions;

    /// <summary>
    /// <b>The decoy wears the CASTER'S creature type.</b>
    /// </summary>
    /// <remarks>
    /// Copied out of the caster's own combat data rather than named by the spell, so the illusion
    /// always looks like whoever cast it. A port that gives the decoy a fixed appearance loses the
    /// whole point of it.
    /// </remarks>
    public static int CreatureTypeFor(int casterCreatureType) => casterCreatureType;

    /// <summary>Health the decoy spawns with.</summary>
    public const int Health = 1;

    /// <summary>Stamina it spawns with.</summary>
    public const int Stamina = 1;

    /// <summary>
    /// Speed it spawns with — <b>zero</b>.
    /// </summary>
    /// <remarks>
    /// <b>It never acts and never moves.</b> The decoy is there to be looked at and attacked, not to
    /// fight; giving it the caster's speed along with the caster's appearance would put a second
    /// combatant on the field.
    /// </remarks>
    public const int Speed = 0;

    /// <summary>Whether the decoy can do anything on its turn.</summary>
    public static bool CanAct => Speed > 0;

    /// <summary>
    /// The flags a decoy spawns with: <b><see cref="CombatantFlags.Ready"/>, and NOT
    /// <see cref="CombatantFlags.AiSummon"/>.</b>
    /// </summary>
    /// <remarks>
    /// This is the mechanical difference from <see cref="MonsterSummon.InitialFlags"/>, which is the
    /// other way round on both bits. A conjured monster is marked as a summon and is not ready; the
    /// decoy is marked ready and is not marked a summon at all — so nothing downstream that keys off
    /// the summon bit will find it.
    ///
    /// <para>Being READY with <see cref="Speed"/> zero is not a contradiction: it takes its place in
    /// the order and has nothing to spend there. That is what makes it a thing to be attacked rather
    /// than a second combatant.</para>
    /// </remarks>
    public const CombatantFlags InitialFlags = CombatantFlags.Ready;

    /// <summary>The morale a decoy spawns with: <b>zero, so it never routs.</b></summary>
    /// <remarks>
    /// Same value and same consequence as <see cref="MonsterSummon.Morale"/> — zero is one of
    /// <see cref="MonsterMorale"/>'s two never-flee values — but it gets there without a guard,
    /// because this routine writes the decoy's stats directly and never calls the MONSTXX.DAT roll.
    /// There is no template here for a zero to have to survive.
    /// </remarks>
    public const int Morale = 0;

    /// <summary>
    /// <b>The decoy is DELIVERED, not simply placed.</b>
    /// </summary>
    /// <remarks>
    /// Between the tile pick and the actor's creation the routine plays the ranged-attack animation
    /// from the caster to the chosen tile, with the caster's own creature type as the projectile —
    /// so the illusion visibly travels there. A port that makes it appear instantly loses the tell
    /// that says which caster it came from.
    /// </remarks>
    public static bool ArrivesOnAProjectile => true;

    /// <summary>
    /// <b>It expires.</b>
    /// </summary>
    /// <remarks>
    /// The decoy takes an active spell-effect slot carrying <see cref="Spell"/> and the cast's
    /// duration — unlike <see cref="MonsterSummon"/>, which sets no slot at all
    /// (<see cref="MonsterSummon.NoEffectSlot"/>) and therefore lasts until killed.
    /// </remarks>
    public static bool ExpiresWithItsSpell => true;

    /// <summary>
    /// <b>The spawn waits for the mouse button to be RELEASED.</b>
    /// </summary>
    /// <remarks>
    /// The placement click is consumed by <see cref="SummonPlacement"/>'s loop, and this spins until
    /// the button comes back up before creating the actor. Without it the same press would carry
    /// through into whatever the new grid state offers next.
    /// </remarks>
    public static bool WaitsForButtonRelease => true;

    /// <summary>Whether this summon asks the player where to put it.</summary>
    /// <remarks>
    /// <b>Yes — and the plain monster summon does not.</b> This is the caller that passes the prompt
    /// flag; a spell-cast monster summon lands on the placement globals without asking.
    /// </remarks>
    public static bool PromptsForTile => true;
}
