namespace GameData.Resources.World;

/// <summary>
/// Where a zone's distance haze starts and where it is full, in BaK units: the range the original's
/// sprite fog remap covers (<c>worldrender_sprite_billboard</c>, WORLDRND.C:238-247). The port uses it
/// for Unity fog rather than reproducing the per-pen remap (TASK-434, JvE 2026-09-12: Unity standards,
/// not pixel-perfect).
/// </summary>
public static class ZoneFog {
    /// <summary>The underground rule's bucket: <c>rung = (dist / 1600) * 2</c>.</summary>
    public const int UndergroundBucket = 1600;

    /// <summary>
    /// Outdoors the haze starts at <see cref="ZoneDefinition.SpriteFogNearDistance"/> and reaches the
    /// last rung one <see cref="ZoneDefinition.SpriteFogDivisor"/> per rung later (Z01: 13000..25000).
    /// Underground (divisor -1) every 1600 units is two rungs (Z10: 3000..6400).
    /// </summary>
    public static (uint Start, uint End) Range(ZoneDefinition zone) {
        uint start = zone.SpriteFogNearDistance;
        uint end = zone.IsUnderground || zone.SpriteFogDivisor <= 0
            ? (uint)(zone.RmpResourceCount / 2 * UndergroundBucket)
            : start + (uint)(zone.RmpResourceCount * zone.SpriteFogDivisor);
        return (start, System.Math.Max(end, start + 1));
    }
}
