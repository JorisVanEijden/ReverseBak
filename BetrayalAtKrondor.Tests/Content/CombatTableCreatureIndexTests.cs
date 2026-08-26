namespace BetrayalAtKrondor.Tests.Content;

using GameData.Resources.World;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

/// <summary>
/// COMBAT.TBL is indexed by <see cref="CreatureType"/>, which is what lets a roaming actor be drawn
/// without inventing a size.
/// </summary>
/// <remarks>
/// <b>How the original reaches it.</b> <c>rgnenc_render_object</c> sets
/// <c>shapeId = g_nProximityTableCount + creatureNumber</c> and draws through the ordinary entity
/// path. <c>ts_get_shape</c> walks the shape-table SLOTS subtracting each one's count
/// (SHAPETBL.C:138-152), and <c>zone_load</c> fills slot 0 from the zone's own <c>Z##.TBL</c> and
/// slot 1 from <c>combat.tbl</c> (ZONE.C:147-148). The zone's GID block has one proximity zone per
/// shape, so <c>g_nProximityTableCount</c> is exactly slot 0's count — and the sum lands on
/// <c>COMBAT.TBL[creatureNumber]</c>.
///
/// <para>So a creature's extent comes from data like every other world sprite's, and the "pick a
/// size" trap has no reason to be entered. This pins the indexing that makes that true; without it
/// the claim lives only in a task note.</para>
/// </remarks>
public class CombatTableCreatureIndexTests {
    // Name in COMBAT.TBL -> the CreatureType whose value must be its index. Deliberately a spread
    // (party, humanoid, beast) rather than a run, so an off-by-one cannot satisfy all of them.
    private static readonly (string Name, CreatureType Type)[] Expected = {
        ("gorath", CreatureType.Gorath),
        ("owyn", CreatureType.Owyn),
        ("mordel", CreatureType.MoredhelWarrior),
        ("blkslay", CreatureType.BlackSlayer),
        ("spider", CreatureType.Spider),
        ("troll", CreatureType.Troll),
    };

    private static JsonElement? Entries() {
        string? root = GeneratedCorpus.FindDir("TBL", "DEF");
        if (root == null) {
            return null;
        }
        string path = Path.Combine(root, "TBL", "COMBAT.json");
        if (!File.Exists(path)) {
            return null;
        }
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.TryGetProperty("Entries", out JsonElement entries)
            ? entries.Clone()
            : (JsonElement?)null;
    }

    [Fact]
    public void EachCreaturesShapeSitsAtItsOwnCreatureNumber() {
        JsonElement? entries = Entries();
        if (entries == null) {
            return;   // no corpus on this machine
        }

        Dictionary<int, string> byIndex = entries.Value.EnumerateArray().ToDictionary(
            e => e.GetProperty("Index").GetInt32(),
            e => e.TryGetProperty("Name", out JsonElement n) ? n.GetString() ?? "" : "");

        foreach ((string name, CreatureType type) in Expected) {
            Assert.True(byIndex.TryGetValue((int)type, out string? found),
                $"COMBAT.TBL has no entry at index {(int)type} for {type}");
            Assert.Equal(name, found);
        }
    }

    [Fact]
    public void EveryCreatureShapeCarriesAnExtentToSizeItsBillboardFrom() {
        JsonElement? entries = Entries();
        if (entries == null) {
            return;
        }

        var seen = 0;
        foreach (JsonElement entry in entries.Value.EnumerateArray()) {
            int index = entry.GetProperty("Index").GetInt32();
            // CreatureType's underlying type is short, and Enum.IsDefined demands the exact
            // underlying type — an int argument throws rather than answering false.
            if (index < short.MinValue || index > short.MaxValue
                || !System.Enum.IsDefined(typeof(CreatureType), (short)index)) {
                continue;   // 0-14 are projectiles, spell effects and crystals
            }
            seen++;
            int extent = entry.GetProperty("Dat").GetProperty("Extent").GetInt32();
            Assert.True(extent > 0, $"creature shape {index} has no extent");
        }

        Assert.True(seen > 20, $"only {seen} creature shapes matched — the indexing may have moved");
    }
}
