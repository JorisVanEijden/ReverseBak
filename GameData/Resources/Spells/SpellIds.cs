namespace GameData.Resources.Spells;

/// <summary>
/// The spells the engine singles out <b>by number</b> rather than by any field in
/// <c>SPELLS.DAT</c>. Each of these is a hard-coded branch somewhere in the cast path, so the
/// catalogue alone cannot tell you they behave differently.
/// </summary>
public static class SpellIds {
    /// <summary>Castable only in a combat encounter's own zone kind, never outdoors.</summary>
    public const int CandleGlow = 2;

    /// <summary>
    /// Refused in an enclosed zone, and — uniquely — its <see cref="Spell.Damage"/> only lands on a
    /// target carrying metal gear. See <c>Spell_CalcEffectMagnitude</c>.
    /// </summary>
    public const int Skyfire = 5;

    /// <summary>Blocked when the combat grid is full, alongside the summoning kinds.</summary>
    public const int DannonsDelusions = 1;

    /// <summary>Refused in an enclosed zone.</summary>
    public const int MadGodsRage = 0x15;

    /// <summary>Refused in an enclosed zone, and outdoors only outside daylight hours.</summary>
    public const int Stardusk = 0x1a;
}
