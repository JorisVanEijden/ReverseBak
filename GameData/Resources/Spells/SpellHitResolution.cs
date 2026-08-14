namespace GameData.Resources.Spells;

/// <summary>
/// Whether a cast connects — the hit-determination step of IDA <c>Cast_Spell</c> (ovr173, around
/// 0x685c0).
/// </summary>
public static class SpellHitResolution {
    /// <summary>
    /// The only targeting type whose casts can miss.
    /// </summary>
    /// <remarks>
    /// See <see cref="CanMiss"/> — everything else auto-applies.
    /// </remarks>
    public const int MissableTargetingType = 0;

    /// <summary>
    /// Whether this cast rolls to hit at all.
    /// </summary>
    /// <param name="targetingType">The spell record's targeting type.</param>
    /// <param name="costWasNegated">Whether the supplied cost arrived negative.</param>
    /// <param name="hasTarget">Whether a target survived the prologue.</param>
    /// <remarks>
    /// <b>Almost nothing can miss.</b> All three conditions must hold — targeting type 0, a real
    /// target, and a cost that was not negative — and otherwise the cast is treated as an automatic
    /// hit. So self-spells, buffs, grid effects and every negative-cost cast simply land.
    ///
    /// <para>The negative-cost exemption is the surprising one: the same sign that
    /// <see cref="SpellCostModifiers.IsNegated"/> strips also takes the cast off the to-hit path
    /// entirely, which is not something the spell record says anywhere.</para>
    /// </remarks>
    public static bool CanMiss(int targetingType, bool costWasNegated, bool hasTarget) =>
        !costWasNegated && targetingType == MissableTargetingType && hasTarget;

    /// <summary>
    /// What a cast that does not roll is treated as.
    /// </summary>
    /// <remarks>A hit — the flag is set to 1 rather than left unset, so nothing downstream has to
    /// distinguish "did not roll" from "rolled well".</remarks>
    public static bool AutomaticResult => true;

    /// <summary>
    /// <b>Spell accuracy reuses the ranged-attack formula.</b>
    /// </summary>
    /// <remarks>
    /// The roll is the same one a crossbow makes — accuracy less twice the range, plus an ammunition
    /// term that is zero for spells — but keyed on the caster's casting skill rather than a weapon
    /// skill. So a spell's chance to land falls off with distance exactly as a bolt's does, which no
    /// part of the spell data hints at.
    /// </remarks>
    public static bool UsesRangedAccuracyFormula => true;

    /// <summary>
    /// Ammunition contribution to a spell's to-hit roll: none.
    /// </summary>
    public const int AmmunitionBonus = 0;
}
