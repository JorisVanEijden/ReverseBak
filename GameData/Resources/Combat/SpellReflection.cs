namespace GameData.Resources.Combat;

/// <summary>
/// A Mirrorwall standing in a spell's flight path takes the cast and throws it back at whoever
/// cast it — <c>combat_actor_tile_entry_effect</c> case 7 (CACTOR.C:1093) feeding
/// <c>cspell_resolve_cast</c>'s pending-tile branch (CSPELL.C:1523).
/// </summary>
/// <remarks>
/// <b>This is what Mirrorwall is FOR.</b> The spell's own arm only paints a terrain kind on a cell
/// (<see cref="Spells.Spell.TileEffectId"/>); everything that makes the wall worth 20 points lives
/// here, in code that never names the spell.
///
/// <para><b>The original arms it from the RENDER loop, and this computes it from the line
/// instead.</b> <c>combat_actor_grid_render</c> calls the tile-entry routine for whatever
/// non-combatant object is being drawn on a cell, so it is the effect sprite passing over the wall
/// that trips the flag. Reproducing that would mean stepping a sprite frame by frame to answer a
/// yes/no question; walking the same line the shot takes gives the same answer, and it is the same
/// line <see cref="CombatLineOfFire"/> already walks for the actor question. The project's rule is
/// to port the behaviour, not the implementation.</para>
///
/// <para><b>The line-of-fire trace does NOT arm it.</b> <c>combat_actor_trace_proj_path</c> calls
/// the same routine with <c>do_apply = 0</c>, and case 7 returns on that before touching the flag.
/// So checking whether a shot is clear cannot set a wall off — only firing at something can.</para>
/// </remarks>
public static class SpellReflection {
    /// <summary>The terrain a Mirrorwall writes, and the only kind that reflects.</summary>
    /// <remarks>
    /// Terrain 7 is not authored data: no <c>TRAPS.DAT</c> record produces it, and the only writer
    /// in the original is the kind-5 cast arm, whose two spells are Mirrorwall (7) and Gambit of the
    /// Eight (8). So a reflecting cell is always one somebody paid for. Gambit's 8 is absent from
    /// the tile-entry switch and falls through <c>default: return</c> — whatever it does, it is not
    /// this.
    /// </remarks>
    public const CombatTerrain Reflector = CombatTerrain.Wall;

    /// <summary>
    /// The one spell a wall will not throw back — Strength Drain (0x2a).
    /// </summary>
    /// <remarks>
    /// <c>cspell_apply_step_tile_spell</c>'s only guard is <c>if (spell_id != 0x2a)</c>, and it
    /// wraps the whole body: an intercepted Strength Drain makes no sound, resolves nothing, and
    /// does not reach its target either. It is swallowed.
    /// </remarks>
    public static bool ReflectsSpell(int spellId) => spellId != Spells.SpellIds.StrengthDrain;

    /// <summary>
    /// Whether this cast puts anything over the grid for a wall to catch.
    /// </summary>
    /// <param name="hasTarget">Whether the cast has a destination actor.</param>
    /// <param name="animationEffectType">The spell record's <c>AnimationEffectType</c>.</param>
    /// <remarks>
    /// <b>Only a spell that flies can be intercepted, and that follows from the arming rather than
    /// being a rule of its own.</b> The flag is set by whatever sprite is drawn over the wall's
    /// cell; a spell whose animation is a palette flash is drawn on the TARGET, and a target cannot
    /// be standing on a wall — the wall's own placement refuses an occupied cell and
    /// <see cref="CombatGrid.IsBlocked"/> keeps anyone from walking onto one afterwards. So the
    /// flying test is the same test <see cref="SpellProjectileSound.Flies"/> makes, for the same
    /// underlying reason.
    /// </remarks>
    public static bool CanBeIntercepted(bool hasTarget, int animationEffectType) =>
        SpellProjectileSound.Flies(hasTarget, animationEffectType);
}
