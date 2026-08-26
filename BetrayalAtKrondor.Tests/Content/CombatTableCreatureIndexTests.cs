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

    // Faces in the first LOD's first six meshes, per creature entry that has geometry.
    private static List<(int Index, string Name, int[] Prefix)> MeshProfiles() {
        var profiles = new List<(int, string, int[])>();
        JsonElement? entries = Entries();
        if (entries == null) {
            return profiles;
        }

        foreach (JsonElement entry in entries.Value.EnumerateArray()) {
            int index = entry.GetProperty("Index").GetInt32();
            if (index < (int)CreatureType.Gorath) {
                continue;   // 0-14 are projectiles, spell effects and crystals
            }
            JsonElement lods = entry.GetProperty("Dat").GetProperty("Lods");
            if (lods.GetArrayLength() == 0) {
                continue;
            }
            JsonElement meshes = lods[0].GetProperty("Meshes");
            profiles.Add((index,
                entry.TryGetProperty("Name", out JsonElement n) ? n.GetString() ?? "" : "",
                meshes.EnumerateArray().Take(6)
                    .Select(m => m.GetProperty("MeshFaceCount").GetInt32()).ToArray()));
        }
        return profiles;
    }

    [Fact]
    public void MeshZeroHoldsTheWALKFacesAndMeshOneTheSTANDINGOnes() {
        // *** WHICH MESH IS WHICH IS THE LAST THING THE DRAW NEEDS. *** EncounterActorPose's
        // walking columns run 0, 3, 6, 9, 12 with three frames each — indices up to 14, so 15
        // faces — and its standing columns are 3, 7, 11, so 12. The shipped profile is exactly
        // that, for the overwhelming majority of creatures.
        List<(int Index, string Name, int[] Prefix)> profiles = MeshProfiles();
        if (profiles.Count == 0) {
            return;
        }

        var standard = profiles.Where(p => p.Prefix.Length >= 6
            && p.Prefix[0] == 15 && p.Prefix[1] == 12 && p.Prefix[2] == 12
            && p.Prefix[3] == 6 && p.Prefix[4] == 3 && p.Prefix[5] == 3).ToList();

        Assert.True(standard.Count >= 30,
            $"only {standard.Count} of {profiles.Count} entries carry the 15/12/12/6/3/3 profile");
    }

    [Fact]
    public void FOURCreaturesAreONEFaceSHORTOfTheWalkRange() {
        // *** A PORT THAT INDEXES BLINDLY GOES OUT OF RANGE ON THESE. *** Column 12 plus frame 2 is
        // index 14, so a 14-face mesh has no face for the rearmost column's last frame. The three
        // wyverns and the spider ship exactly that. Whether the original reads a missing face there
        // or those four use a different column set is NOT established — this pins the condition so
        // it is met deliberately rather than as a crash.
        List<(int Index, string Name, int[] Prefix)> profiles = MeshProfiles();
        if (profiles.Count == 0) {
            return;
        }

        var short14 = profiles
            .Where(p => p.Prefix.Length >= 1 && p.Prefix[0] == 14)
            .Select(p => p.Name).OrderBy(n => n).ToList();

        Assert.Equal(new[] { "spider", "wyvern", "wyvern", "wyvern" }, short14);
    }

    [Fact]
    public void SomeEntriesInTheCreatureRangeAreNOTCreatures() {
        // rock, dots, blackcry, spell5, spell6 and sling sit at creature indices and carry a single
        // one-face mesh. An index being in range is not proof it names a creature — the roster and
        // the encounter record decide that, not the table's extent.
        List<(int Index, string Name, int[] Prefix)> profiles = MeshProfiles();
        if (profiles.Count == 0) {
            return;
        }

        var props = profiles.Where(p => p.Prefix.Length >= 1 && p.Prefix[0] == 1)
            .Select(p => p.Name).ToList();

        Assert.Contains("rock", props);
        Assert.Contains("sling", props);
    }
}
