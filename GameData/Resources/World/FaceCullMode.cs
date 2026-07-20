namespace GameData.Resources.World;

/// <summary>
/// How a world polygon face is culled, decoded from <see cref="PolygonFace.Flags"/> bits 0-1
/// (see ZoneTable-DAT.md §4; backfaceCullDotProduct 0x239d4).
/// </summary>
public enum FaceCullMode
{
    /// <summary>Raw bits 0. Rendered from both sides.</summary>
    DoubleSided = 0,

    /// <summary>Raw bits 1 or 3. Backface-culled against the face normal.</summary>
    SingleSided = 1,

    /// <summary>Raw bits 2. Not rendered at all — the original skips these outright.</summary>
    Skip = 2,
}
