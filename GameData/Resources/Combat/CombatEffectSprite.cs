namespace GameData.Resources.Combat;

/// <summary>
/// Which COMBAT.TBL entry is drawn flying from an attacker to its target.
/// </summary>
/// <remarks>
/// The original passes an effect-sprite id to the flight routine at four call sites, and every one
/// of the four ids lands on an aptly named COMBAT.TBL entry — checked against the shipped table,
/// which is what turned these from "an integer in an instruction encoding" into a mapping worth
/// keeping: 2 = <c>spell</c>, 3 = <c>jack</c>, 20 = <c>rock</c>, 50 = <c>spell5</c>.
/// <para>The two spells that get their own sprite are picked by id, not by any property of the
/// spell record — the original branches on the spell number.</para>
/// </remarks>
public static class CombatEffectSprite {
    /// <summary>A crossbow quarrel — COMBAT.TBL <c>rock</c>.</summary>
    public const int Shot = 20;

    /// <summary>Flamecast's own sprite — COMBAT.TBL <c>spell</c>.</summary>
    public const int Flamecast = 2;

    /// <summary>Bane of Black Slayers' own sprite — COMBAT.TBL <c>jack</c>.</summary>
    public const int BaneOfBlackSlayers = 3;

    /// <summary>Every other spell — COMBAT.TBL <c>spell5</c>.</summary>
    public const int GenericSpell = 50;

    private const int FlamecastSpellId = 4;
    private const int BaneOfBlackSlayersSpellId = 9;

    /// <summary>The effect sprite a cast of <paramref name="spellId"/> flies.</summary>
    public static int ForSpell(int spellId) => spellId switch {
        FlamecastSpellId => Flamecast,
        BaneOfBlackSlayersSpellId => BaneOfBlackSlayers,
        _ => GenericSpell,
    };
}
