namespace GameData.Resources.Spells;

/// <summary>
/// Thy Master's Will (spell 41) — the effect-kind-18 arm of <c>cspell_invoke_effect</c>
/// (canassa CSPELL.C:852): a wyvern it lands on turns and runs.
/// </summary>
/// <remarks>
/// For a target of creature type 0x29-0x2b the arm plays 0x51, adds status 0xc, sets morale 1,
/// routes the creature (<c>combatenc_actor_flee_tile_east</c>) and consumes one of the spell's
/// inventory component from the caster. Anything else is untouched — no cue, no egg spent.
/// Status 0xc has no reader anywhere in the source, so it is not modelled.
/// </remarks>
public static class ThyMastersWill {
    /// <summary><c>pSpell->nEffect_kind</c> for this spell.</summary>
    public const int EffectKind = 18;

    public const int FirstAffectedCreatureType = 0x29;
    public const int LastAffectedCreatureType = 0x2b;

    /// <summary>Whether the arm acts on a creature of this type.</summary>
    public static bool Affects(int creatureType) =>
        creatureType >= FirstAffectedCreatureType && creatureType <= LastAffectedCreatureType;
}
