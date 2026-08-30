namespace GameData.Resources.Combat;

using System;

/// <summary>
/// The damage a spell field on the floor deals to whoever stands in it —
/// <c>cspell_tick_damage_terrain</c> (canassa CSPELL.C:2427).
/// </summary>
/// <remarks>
/// <b>ONE KIND BURNS, AND IT IS THE WRATH OF KILLIAN'S.</b> The sweep visits all 8x13 cells and
/// its switch has exactly one case: terrain kind 1. Everything else a cell can be — crystal,
/// cannon, trap, the pushable diamond — is walked over without damage. So a port that treats
/// "there is an effect on this tile" as "it hurts" invents harm the game does not deal.
///
/// <para>Kind 1 is not authored terrain at all; it is painted at runtime by spell 19, Wrath of
/// Killian — see <see cref="CombatCapability.DenyingTerrain"/> for the chain. That is also why
/// standing in it denies shooting and casting: you are standing in fire.</para>
/// </remarks>
public static class TerrainDamage {
    /// <summary>The only terrain kind that damages its occupant.</summary>
    public const int BurningKind = CombatCapability.DenyingTerrain;

    /// <summary>The spell whose resistance is consulted before the damage lands.</summary>
    /// <remarks>
    /// <c>0x13</c> — Wrath of Killian itself, the spell that paints the field. So resisting the
    /// spell resists its residue, which is the only sense in which a floor has an element.
    /// </remarks>
    public const int ResistedAsSpell = 0x13;

    /// <summary>Lowest damage a burning tile deals per tick.</summary>
    public const int MinimumDamage = 10;

    /// <summary>Highest damage a burning tile deals per tick.</summary>
    /// <remarks>
    /// <c>RNDR(10, 19)</c> — inclusive at both ends, so ten possible values rather than nine.
    /// </remarks>
    public const int MaximumDamage = 19;

    /// <summary>Whether an occupied cell of this kind damages its occupant.</summary>
    /// <param name="terrainKind">The cell's terrain word.</param>
    /// <param name="occupied">Whether anybody is standing there.</param>
    /// <param name="occupantResists">
    /// The occupant's creature resistance to <see cref="ResistedAsSpell"/>.
    /// </param>
    /// <remarks>
    /// <b>An empty burning tile does nothing and is not an error.</b> The original tests the
    /// occupant before the switch, so a field with nobody in it simply burns down.
    /// </remarks>
    public static bool Burns(int terrainKind, bool occupied, bool occupantResists) =>
        occupied && terrainKind == BurningKind && !occupantResists;

    /// <summary>The damage one tick of standing in it deals.</summary>
    /// <param name="rnd"><c>rnd(n)</c> returns a value in <c>[0, n)</c>.</param>
    public static int DamageFor(Func<int, int> rnd) {
        if (rnd == null) {
            throw new ArgumentNullException(nameof(rnd));
        }
        return MinimumDamage + rnd(MaximumDamage - MinimumDamage + 1);
    }
}
