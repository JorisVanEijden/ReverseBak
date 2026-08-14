namespace GameData.Resources.World;

/// <summary>
/// The palette lerp every lighting step goes through — IDA <c>BlendPaletteColors</c>
/// (seg031 @0x2cf68) and the table it precomputes in <c>initializeInterpolationTables</c>
/// (@0x2d05f).
/// </summary>
public static class PaletteBlend {
    /// <summary>Full scale for both a channel and a light level — VGA's six bits.</summary>
    public const int Scale = 64;

    /// <summary>Largest value a palette channel holds.</summary>
    public const int MaxChannel = 63;

    /// <summary>
    /// How far toward the target colour a light level actually moves.
    /// </summary>
    /// <remarks>
    /// <b>The parameter the original calls a "blend factor" is a LIGHT level, and the blend uses its
    /// complement</b> — a higher number means <i>less</i> effect, not more. Read it the obvious way
    /// round and every lighting effect inverts: bright scenes go black and dark ones stay lit.
    /// </remarks>
    public static int EffectOf(int lightLevel) => Scale - lightLevel;

    /// <summary>
    /// Whether the blend does nothing but copy.
    /// </summary>
    /// <remarks>
    /// <b>Both ends are pass-through.</b> A light level of 64 leaves the palette alone because there
    /// is nothing to darken, and a level of 0 leaves it alone too — the effect is clamped out rather
    /// than saturating to the target. So "no light" does not mean "black"; it means "unchanged", and
    /// the darkness you see at night comes from levels in between, never from the bottom of the
    /// range.
    /// </remarks>
    public static bool IsPassThrough(int lightLevel) {
        int effect = EffectOf(lightLevel);

        return effect <= 0 || effect >= Scale;
    }

    /// <summary>
    /// One channel of one palette entry.
    /// </summary>
    /// <remarks>
    /// <c>source + (target − source) × (64 − light) / 64</c>, and the division <b>truncates toward
    /// zero</b>: the original precomputes the magnitude and negates it, so a channel moving down
    /// rounds the same way as one moving up.
    /// </remarks>
    public static int Channel(int source, int target, int lightLevel) {
        if (IsPassThrough(lightLevel)) {
            return source;
        }

        int delta = target - source;
        int effect = EffectOf(lightLevel);
        int step = (delta < 0 ? -((-delta * effect) / Scale) : (delta * effect) / Scale);

        return source + step;
    }

    /// <summary>
    /// <b>The lookup is one table read for both directions.</b>
    /// </summary>
    /// <remarks>
    /// The original indexes a "negative" table by <c>target − source + 63</c>. For a target below
    /// the source that lands inside it; for a target above, it runs off the end — straight into the
    /// "positive" table, which is laid out immediately after it, exactly 63 bytes on. The overrun is
    /// deliberate and is what lets one index serve both signs. A port that keeps two arrays with
    /// bounds checks does not reproduce it; a port that keeps two arrays and indexes the wrong one
    /// gets the sign backwards.
    /// </remarks>
    public static bool TablesAreContiguous => true;

    /// <summary>
    /// The table is rebuilt only when the light level changes from the previous call — the original
    /// caches on exactly that.
    /// </summary>
    public static bool TableIsCachedByLevel => true;
}
