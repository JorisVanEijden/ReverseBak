namespace GameData.Resources.World;

// Parsed Tzzxxyy.DAT — encounter/event triggers for one world tile, keyed
// by chapter. Each trigger fires when the player's sub-tile coordinate is
// inside the trigger's rectangle and the trigger's gating keys allow it.
//
// Gating (universal, ovr187:sub_532):
//   - Skipped if GetGlobal5200_5209() != 0 (already-handled flag).
//   - Skipped if Forbids condition holds (decoded from on-disk ForbiddenKey).
//   - Required: Requires is null OR Requires condition holds (decoded from on-disk RequiredKey).
//
// On-fire (per type handler):
//   - OnFire effect applied if present (decoded from on-disk SetOnFireKey).
//   - Type-specific side-effect (LoadEntryFromDefFile + dispatch).
public class TileEventTile : IResource
{
    public TileEventTile(string id) { Id = id; }

    public byte ZoneNumber { get; set; }
    public byte X { get; set; }
    public byte Y { get; set; }

    public List<TileEventChapter> Chapters { get; set; } = new();

    public ResourceType Type => ResourceType.DAT;
    public string Id { get; }
}
