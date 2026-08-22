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
public static class DecoySummon {
    /// <summary>The spell the decoy's effect slot is stamped with.</summary>
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
