namespace GameData.Resources.Combat;

using System;

/// <summary>
/// Where a MISSED spell projectile goes — <c>world_rndr_ranged_attack_anim</c> (WORLDHIT.C:585)
/// with <c>*p_hit_out == 0</c>.
/// </summary>
/// <remarks>
/// <b>A miss is not a no-op; it is a different flight.</b> The heading is turned by a random
/// ±[0x400, 0x6FF] (5.6°–9.8°), the step count becomes 10,000, and the shot flies on at 250 units a
/// frame until it leaves the view — asking at every step who stands in the cell it has reached. The
/// first living combatant that is neither the caster nor the aimed target is STRUCK: the routine
/// returns it and sets the hit flag, and <c>cspell_apply_hit_at</c> (CSPELL.C:402) writes it back as
/// the target, so the burst, the damage and Flamecast's splash all land on the bystander
/// (WORLDHIT.C:614-729). Seen in the original on 2026-09-26: a missed Flamecast veered off past
/// its target and flew out of view.
///
/// <para><b>The flight ends at the grid edge here, where the original ends at the screen edge.</b>
/// ponytail: the arena camera shows the whole 8x13 board, so a shot leaves the screen at or just past
/// the grid; nobody stands outside it to be struck, so the outcome is the same and only the length of
/// the drawn flight differs.</para>
/// </remarks>
public static class SpellProjectileMiss {
    /// <summary>The flight speed <c>cspell_apply_hit_at</c> passes: <c>0xfa</c> world units a frame.</summary>
    public const int StepUnits = 0xfa;

    /// <summary><c>RNDR(0x400, 0x6ff)</c>, in 16-bit heading units (0x10000 = a full turn).</summary>
    public const int DeflectMin = 0x400, DeflectMax = 0x6ff;

    /// <summary>The miss's step budget — effectively "until something stops it".</summary>
    public const int MaxSteps = 10000;

    /// <param name="Intercepted">The cell of the bystander struck, or null when nobody was.</param>
    /// <param name="EndX">Where the flight stopped, in cell units (fractional; may lie off the grid).</param>
    public readonly record struct Result((int X, int Y)? Intercepted, float EndX, float EndY, int Steps);

    /// <summary>Fly a missed shot from one cell towards another.</summary>
    /// <param name="rnd">The original's <c>RND(n)</c>: uniform in [0, n). Rolled twice — sign, then size.</param>
    /// <param name="strikes">Whether a cell holds a combatant this shot would strike — alive, and not
    /// the caster or the aimed target (WORLDHIT.C:696-699).</param>
    public static Result Fly(int fromX, int fromY, int toX, int toY, int cellSize, Func<int, int> rnd,
        Func<int, int, bool> strikes) {
        if (cellSize <= 0) {
            return new Result(null, toX + 0.5f, toY + 0.5f, 0);
        }
        int sign = rnd(2) == 0 ? -1 : 1;
        int deflect = sign * (DeflectMin + rnd(DeflectMax - DeflectMin + 1));
        double heading = Math.Atan2(toY - fromY, toX - fromX) + deflect * 2 * Math.PI / 0x10000;
        double x = fromX * cellSize + cellSize / 2.0;
        double y = fromY * cellSize + cellSize / 2.0;
        double dx = StepUnits * Math.Cos(heading), dy = StepUnits * Math.Sin(heading);
        for (var step = 1; step <= MaxSteps; step++) {
            x += dx;
            y += dy;
            int cx = (int)Math.Floor(x / cellSize), cy = (int)Math.Floor(y / cellSize);
            if (!CombatGrid.InBounds(cx, cy)) {
                return new Result(null, (float)(x / cellSize), (float)(y / cellSize), step);
            }
            if (strikes(cx, cy)) {
                return new Result((cx, cy), cx + 0.5f, cy + 0.5f, step);
            }
        }
        return new Result(null, (float)(x / cellSize), (float)(y / cellSize), MaxSteps);
    }
}
