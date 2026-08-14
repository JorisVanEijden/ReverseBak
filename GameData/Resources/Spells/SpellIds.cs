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

    /// <summary>Hands its whole effect to <c>Cast_Flamecast</c> after the animation.</summary>
    public const int Flamecast = 4;

    /// <summary>The effect <see cref="FettersOfRime"/> registers instead of its own.</summary>
    public const int GriefOfAThousandNights = 13;

    /// <summary>
    /// Ends its own cast when the caster is already beside the target; otherwise walks the caster in.
    /// </summary>
    public const int TouchOfLimsKragma = 15;

    /// <summary>Has its magnitude replaced wholesale after the animation.</summary>
    public const int UnfortunateFlux = 20;

    /// <summary>Ends its own cast — <c>Cast_Winds_of_Eortis</c> is the entire spell.</summary>
    public const int WindsOfEortis = 27;

    /// <summary>Throws its computed magnitude away after the animation.</summary>
    public const int Firestorm = 28;

    /// <summary>Kills the target outright after the animation rather than damaging it.</summary>
    public const int FinalRest = 32;

    /// <summary>Registers Grief of 1000 Nights on the target instead of an effect of its own.</summary>
    public const int FettersOfRime = 36;
}
