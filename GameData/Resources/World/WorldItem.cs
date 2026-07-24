namespace GameData.Resources.World;

public class WorldItem
{
    public ushort TypeId { get; set; }

    /// <summary>Stable content-graph key of the zone TBL entity this placement instances:
    /// <c>base:tbl:z&lt;zone&gt;:&lt;TypeId&gt;</c> (e.g. <c>base:tbl:z09:45</c>). De-indexes the raw
    /// <see cref="TypeId"/> (a positional index into the zone TBL, which breaks under additive merge)
    /// into a reference that survives mods reordering/extending the table. Resolves to
    /// <c>ZoneTableEntry.Key</c>. See docs/re-notes/reference-inventory.md #1.</summary>
    public string EntityKey { get; set; } = "";

    public Rotation3D Rotation { get; set; } = new();
    public Position3D Position { get; set; } = new();
}
