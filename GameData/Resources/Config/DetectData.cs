using GameData.Resources.World;

namespace GameData.Resources.Config;

/// <summary>
/// Engine-independent view of DETECT.DAT — the world-item <b>interaction-detection
/// range</b> table, the sibling of <see cref="FilterData"/>. For each location class
/// (aboveground / underground) the game keeps a per-entity-type maximum distance within
/// which an item of that type becomes "detected" — i.e. eligible to show an on-screen
/// interaction marker and be the click target. A range of <c>0</c> means the type is never
/// interactable (purely decorative).
///
/// On-disk layout (344 bytes = 2 blocks × 172 bytes):
///   block 0 = aboveground, block 1 = underground; each = 43 × little-endian i32, one per
///   entity-type tag (0..42). Detection ranges are shorter underground.
///
/// Reversed from <c>Load_detect.dat</c> (ovr190 @ 0x76510): it <c>fseek</c>s to offset 172
/// when <c>zoneLocation == underground</c> and reads 43 dwords into the global
/// <c>data_detect_dat</c> (0x3f1c8); it also allocates a zeroed 10×16-byte scratch list
/// (<c>p10times16bytes</c> @ 0x3f274) that holds up to 10 currently-detected interactables.
/// The render/detect loop <c>sub_seg021_38D</c> (0x21a9d) computes each visible item's
/// distance and keeps it as interactable when <c>distance &lt;= detectRange[EntityType]</c>
/// (EntityType = TableDatInfo +0x01); the same range table is consumed by
/// <c>HandleEnvironmentInteraction_impl</c> (ovr190 0x76573) and <c>sub_ovr137_7BF</c>.
///
/// Distances are in game units (64,000 per tile) and are left unscaled, like all
/// world-space quantities. See <c>docs/FileFormats/DETECT.DAT.md</c> for the entity-type
/// tag → category mapping (shared with FILTER.DAT).
/// </summary>
public class DetectData : IResource {
    /// <summary>Number of location blocks (0 = aboveground, 1 = underground).</summary>
    public const int LocationCount = 2;

    /// <summary>Number of entity-type tags per block (TableDatInfo.EntityType 0..42).</summary>
    public const int EntityTypeCount = 43;

    public DetectData(string id) {
        Id = id;
    }

    public string Id { get; }
    public ResourceType Type => ResourceType.DAT;

    /// <summary>One entry per location class (index 0 = aboveground, 1 = underground).</summary>
    public List<DetectLocationRanges> Locations { get; set; } = new();

    /// <summary>
    /// The DETECT.DAT interaction range for an entity type in a location, or 0 when it has none.
    /// </summary>
    /// <remarks>
    /// <c>underground = false</c> selects <c>Locations[0]</c> (aboveground), <c>true</c> selects
    /// <c>Locations[1]</c>. Out-of-range types and a missing location block both read 0, so a caller
    /// never has to bounds-check the table itself — which is what the duplicate lookup in
    /// <c>WorldEntityBuilder.StampDetectionRange</c> was doing.
    /// </remarks>
    public int GetRange(WorldEntityType type, bool underground) {
        int block = underground ? 1 : 0;
        if (block >= Locations.Count) {
            return 0;
        }
        int[] ranges = Locations[block].DetectRanges;
        int idx = (byte)type;
        return idx < ranges.Length ? ranges[idx] : 0;
    }

    /// <summary>An entity type is interactable in a location when its DETECT.DAT range is &gt; 0.</summary>
    public bool IsInteractable(WorldEntityType type, bool underground) =>
        GetRange(type, underground) > 0;
}

/// <summary>
/// The 43 per-entity-type interaction-detection ranges for one location class.
/// </summary>
public class DetectLocationRanges {
    /// <summary>"Aboveground" (block 0) or "Underground" (block 1).</summary>
    public string Location { get; set; } = "";

    /// <summary>
    /// Interaction-detection range in game units, indexed by <c>TableDatInfo.EntityType</c>
    /// (0..42). An item is interactable when <c>distanceToPlayer &lt;= value</c>; <c>0</c> =
    /// never interactable.
    /// </summary>
    public int[] DetectRanges { get; set; } = new int[DetectData.EntityTypeCount];
}
