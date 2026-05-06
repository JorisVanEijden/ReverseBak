namespace GameData.Resources.World;

// Parsed Tzzxxyy.DAT — encounter/event triggers for one world tile, keyed
// by chapter. Each trigger fires when the player's sub-tile coordinate is
// inside the trigger's rectangle and the trigger's gating keys allow it.
//
// Gating (universal, ovr187:sub_532):
//   - Skipped if GetGlobal5200_5209() != 0 (already-handled flag).
//   - Skipped if ForbiddenKey != 0 and GetGlobalValue(ForbiddenKey) != 0.
//   - Required:  RequiredKey == 0  OR  GetGlobalValue(RequiredKey) != 0.
//
// On-fire (per type handler):
//   - SetOnFireKey != 0: SetGlobalValue(SetOnFireKey, 1).
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
