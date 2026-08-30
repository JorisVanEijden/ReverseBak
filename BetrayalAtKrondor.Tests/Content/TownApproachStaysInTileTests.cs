namespace BetrayalAtKrondor.Tests.Content;

using System.IO;
using System.Text.Json;
using GameData.Resources.Scene;
using GameData.Resources.World;
using Xunit;

/// <summary>
/// Whether a town approach can walk the party out of its own map tile.
/// </summary>
/// <remarks>
/// <b>The TYPE allows it and the DATA never does, which is exactly the kind of gap worth pinning.</b>
/// <see cref="TownApproach.SubTileX"/> unpacks a BYTE — 0..255 — while a tile is only
/// <see cref="WorldPlacement.SubCellsPerTile"/> (40) sub-cells across, so an offset of 40 or more
/// would place the party in the NEXT tile along.
///
/// <para>It matters beyond the geometry: the town approach writes the party position directly rather
/// than through <c>PartyMovement</c>, so it raises no <c>Relocated</c> and the world's tile residency
/// (TASK-130) does not follow it. That is only safe while every approach stays inside its own tile —
/// which is a fact about the shipped records, not about the format.</para>
///
/// <para>Skip-if-absent, like the other corpus tests.</para>
/// </remarks>
public class TownApproachStaysInTileTests {
    [Fact]
    public void NoSHIPPEDTownApproachLeavesThePartysOwnTile() {
        string? gen = GeneratedCorpus.FindDir("DEF");
        if (gen == null) {
            return;
        }

        string path = Path.Combine(gen, "DEF", "DEF_TOWN.json");
        if (!File.Exists(path)) {
            return;
        }

        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
        var checkedAny = false;
        // The DEF family serialises its rows under "Records", not the "Entries" the zone tables use.
        foreach (JsonElement entry in doc.RootElement.GetProperty("Records").EnumerateArray()) {
            if (!entry.TryGetProperty("Payload", out JsonElement payload)
                || !payload.TryGetProperty("ApproachTileOffset", out JsonElement offsetElement)) {
                continue;
            }

            int offset = offsetElement.GetInt32();
            checkedAny = true;
            Assert.InRange(TownApproach.SubTileX(offset), 0, WorldPlacement.SubCellsPerTile - 1);
            Assert.InRange(TownApproach.SubTileY(offset), 0, WorldPlacement.SubCellsPerTile - 1);
        }

        Assert.True(checkedAny, "DEF_TOWN.json carried no approach offsets to check");
    }

    [Fact]
    public void ButTheFormatWOULDAllowIt_WhichIsWhyTheAboveIsAsserted() {
        // A byte against 40 sub-cells: 40 lands exactly on the next tile's origin and 255 is six
        // tiles away. So this is a property of the shipped DATA, and a mod authoring an offset of
        // 40 or more would both move the party into the neighbouring tile and leave the world's
        // tile residency centred on the old one.
        Assert.Equal(40, WorldPlacement.SubCellsPerTile);
        Assert.Equal(255, TownApproach.SubTileX(0xFF));
        Assert.Equal(WorldPlacement.TileSize,
            WorldPlacement.CornerOf(0, WorldPlacement.SubCellsPerTile));
    }
}
