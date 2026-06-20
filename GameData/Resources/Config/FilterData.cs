namespace GameData.Resources.Config;

/// <summary>
/// Engine-independent view of FILTER.DAT — the world-item <b>draw-distance / culling</b>
/// table. For each graphics detail-level preference the game keeps a per-entity-type
/// maximum distance at which an item of that type is still added to the render / world-scan
/// set, so distant clutter is dropped first and the detail-level slider trades draw distance
/// for performance.
///
/// On-disk layout (688 bytes = 4 blocks × 172 bytes):
///   block b (b = 0..3) = 43 × little-endian i32, one per entity-type tag (0..42).
/// One block per <see cref="DetailLevels"/> entry; the file holds every level so it can be
/// extracted whole even though the running game only loads the single block matching the
/// current detail-level preference.
///
/// Reversed from <c>Load_filter_dat</c> (ovr136 @ 0x43f60): it <c>fseek</c>s to
/// <c>config.levelOfDetail * 0xAC</c> and reads 43 dwords into the global
/// <c>filterData_43dwords</c> (0x3eae0). The four ovr136 consumers
/// (<c>sub_ovr136_CB</c> 0x4402b, <c>sub_ovr136_231</c> 0x44191, <c>sub_ovr136_42B</c>
/// 0x4438b, <c>sub_ovr136_849</c> 0x447a9) all index the block by the entity's
/// <c>TableDatInfo.EntityType</c> tag (TBL DAT-section +0x01) and, per item, compute
/// <c>worldExtent = Extent &lt;&lt; VertexScale</c> (TBL +0x0C / +0x03) and keep the item when
/// <c>distanceToPlayer - worldExtent &lt; threshold</c>.
///
/// Threshold sentinels (distances are in game units — 1600 per sub-tile, 64000 per tile —
/// and are left unscaled, like all world-space quantities):
///   <c>-1</c> (0xFFFFFFFF) → entity type is never drawn,
///   <c>1</c>              → always drawn (the distance test is bypassed),
///   <c>&gt; 1</c>            → drawn only when the item's near edge is within the distance.
/// (Entity-type 7 is additionally hard-skipped by the scan loop regardless of its value.)
/// See <c>docs/FileFormats/FILTER.DAT.md</c> for the entity-type tag → category mapping.
/// </summary>
public class FilterData : IResource {
    /// <summary>Number of graphics detail-level presets (one 172-byte block each).</summary>
    public const int DetailLevelCount = 4;

    /// <summary>Number of entity-type tags addressed by each block (TableDatInfo.EntityType 0..42).</summary>
    public const int EntityTypeCount = 43;

    public FilterData(string id) {
        Id = id;
    }

    public string Id { get; }
    public ResourceType Type => ResourceType.DAT;

    /// <summary>One entry per graphics detail-level preference (index = <c>config.levelOfDetail</c>,
    /// 0 = lowest detail / shortest draw distance .. 3 = highest).</summary>
    public List<DetailLevelFilter> DetailLevels { get; set; } = new();
}

/// <summary>
/// The 43 per-entity-type draw-distance thresholds for one detail level.
/// </summary>
public class DetailLevelFilter {
    /// <summary>Detail-level index this block applies to (= <c>config.levelOfDetail</c>).</summary>
    public int Level { get; set; }

    /// <summary>
    /// Culling distance in game units, indexed by <c>TableDatInfo.EntityType</c> (0..42).
    /// <c>-1</c> = never drawn, <c>1</c> = always drawn (distance test bypassed),
    /// <c>&gt; 1</c> = drawn when <c>distanceToPlayer - (Extent &lt;&lt; VertexScale) &lt; value</c>.
    /// </summary>
    public int[] DrawDistances { get; set; } = new int[FilterData.EntityTypeCount];
}
