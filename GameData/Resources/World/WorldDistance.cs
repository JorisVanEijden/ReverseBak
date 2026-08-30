namespace GameData.Resources.World;

/// <summary>
/// The engine's distance approximation — <c>distdir_octagonal_distance_dxdy</c>
/// (<c>SRC/R3D/CORE/DISTDIR.ASM</c>).
///
/// <para><b>It is an R3D core routine, not a fare calculator.</b> It lives here rather than beside
/// the teleport prices because the original's callers are spread across the engine: the combat grid
/// (<c>CMBTGRID.C</c>:450), actor spawning (<c>ACTSPAWN.C</c>:324), the proximity scan
/// (<c>PROXSCAN.C</c>:93), projectile hit testing (<c>WORLDHIT.C</c>:663) and world sprite
/// rendering (<c>WORLDRND.C</c>). <see cref="Location.TeleportCost"/> is one caller among many and
/// used to be the only one that had it.</para>
/// </summary>
public static class WorldDistance {
    /// <summary>
    /// <c>max + min * 3 / 8</c>, on the absolute deltas.
    /// </summary>
    /// <remarks>
    /// <b>Deliberately not Euclidean, and reproducing it matters.</b> The 3/8 term makes a pure
    /// diagonal cost about 1.375x a straight line where the true figure is 1.414x, so diagonals
    /// come out slightly short. Every distance the game compares against a threshold — a teleport
    /// fare, a stash's cover and traffic weights, whether a sprite is close enough to draw — is
    /// this number, so substituting a hypotenuse moves all of those thresholds at once.
    ///
    /// <para>The assembly divides with <c>shr ecx,3</c> after taking both absolutes, so the shift
    /// is on a non-negative value and an integer division is equivalent. Written as a division
    /// because that is what it means; a shift on a signed C int would round the other way for
    /// negatives and only look the same until something passed one.</para>
    /// </remarks>
    public static int Octagonal(int dx, int dy) {
        int a = dx < 0 ? -dx : dx;
        int b = dy < 0 ? -dy : dy;
        int max = a < b ? b : a;
        int min = a < b ? a : b;
        return max + (min * 3 / 8);
    }

    /// <summary>The same distance between two points — <c>distdir_octagonal_distance</c>.</summary>
    public static int Between(int x1, int y1, int x2, int y2) => Octagonal(x1 - x2, y1 - y2);
}
