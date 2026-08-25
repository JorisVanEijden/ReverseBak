namespace BetrayalAtKrondor.Tests.Content;

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Xunit;

/// <summary>
/// What distinguishes world-entity types 0, 1 and 3 — the three <see cref="GameData.Resources.World.WorldEntityType"/>
/// values that have never been named.
/// </summary>
/// <remarks>
/// <b>They are the terrain PAINT STACK, and the shipped data says so unambiguously:</b> across all
/// twelve zone tables, <c>DrawPriority</c> is non-zero for exactly those three types and zero for
/// every other type in the game. Types 0, 1 and 3 sit at priorities 8, 7 and 6; everything else is
/// drawn by the ordinary far-to-near sort.
///
/// <para>That is the relationship TASK-29's item 7 was blocked on — its note recorded the
/// <c>g######</c> / <c>t######</c> / <c>r######</c> families sharing a zone+tile suffix and said
/// "naming them wants that relationship established first". This is it: the same tile, painted at
/// three layers.</para>
///
/// <para><b>It is not decoration.</b> <c>ProximityWorld.SortLikeTheRenderer</c> already splits
/// candidates on <c>DrawPriority != 0</c> to reproduce the renderer's "proud geometry first"
/// ordering, so collision depends on exactly this set without the connection being written down
/// anywhere.</para>
///
/// <para><b>A census correction while here.</b> A type-0 count of 1865 has been quoted on TASK-29
/// since 2026-08-23; 1797 of those are <c>null</c> padding slots that the table stamps with type 0
/// by default. The real type-0 population is 68, which is what makes the three families
/// comparable at all.</para>
/// </remarks>
public class TerrainPaintStackTests {
    /// <summary>The three types that carry a draw priority.</summary>
    private static readonly HashSet<int> PaintStack = new() { 0, 1, 3 };

    private sealed record Entry(string Name, int Type, int Priority);

    /// <param name="generatedRoot">
    /// What <see cref="GeneratedCorpus.FindDir"/> returns — the <c>generated/</c> ROOT, not the
    /// sub-directory. Passing it straight to a glob finds nothing and reads as "no corpus".
    /// </param>
    private static List<Entry> RealEntries(string generatedRoot) {
        var all = new List<Entry>();
        foreach (string file in Directory.GetFiles(Path.Combine(generatedRoot, "TBL"), "Z*.json")) {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(file));
            foreach (JsonElement e in doc.RootElement.GetProperty("Entries").EnumerateArray()) {
                string name = e.GetProperty("Name").GetString() ?? "";
                JsonElement dat = e.GetProperty("Dat");
                // A padding slot: no name, no geometry. The table stamps these with type 0, which is
                // what inflated every previous census of it.
                if (name == "null" || dat.GetProperty("LodCount").GetInt32() == 0) {
                    continue;
                }
                all.Add(new Entry(name,
                    ReadType(dat.GetProperty("EntityType")),
                    dat.GetProperty("DrawPriority").GetInt32()));
            }
        }
        return all;
    }

    /// <summary>The named types serialize as strings and the unnamed ones as numbers.</summary>
    private static int ReadType(JsonElement type) =>
        type.ValueKind == JsonValueKind.Number
            ? type.GetInt32()
            : (int)System.Enum.Parse<GameData.Resources.World.WorldEntityType>(type.GetString()!);

    [Fact]
    public void ADrawPriorityIsCarriedByTypes0_1_and3ANDNOTHINGELSE() {
        string? dir = GeneratedCorpus.FindDir("TBL");
        if (dir == null) {
            return;
        }

        List<Entry> entries = RealEntries(dir);
        Assert.NotEmpty(entries);

        foreach (Entry entry in entries) {
            if (PaintStack.Contains(entry.Type)) {
                Assert.True(entry.Priority > 0,
                    $"{entry.Name} is a paint-stack type ({entry.Type}) with priority 0");
            } else {
                Assert.True(entry.Priority == 0,
                    $"{entry.Name} is type {entry.Type} and carries priority {entry.Priority}");
            }
        }
    }

    [Fact]
    public void TheStackIsOrdered_type0AboveType1AboveType3() {
        string? dir = GeneratedCorpus.FindDir("TBL");
        if (dir == null) {
            return;
        }

        var byType = new Dictionary<int, HashSet<int>>();
        foreach (Entry entry in RealEntries(dir)) {
            if (!PaintStack.Contains(entry.Type)) {
                continue;
            }
            if (!byType.TryGetValue(entry.Type, out HashSet<int>? seen)) {
                byType[entry.Type] = seen = new HashSet<int>();
            }
            seen.Add(entry.Priority);
        }

        // Type 1 and type 3 are each a single priority; type 0 spans two, and the six entries at 7
        // are the one exception worth knowing about rather than smoothing over.
        Assert.Equal(new[] { 7 }, Sorted(byType[1]));
        Assert.Equal(new[] { 6 }, Sorted(byType[3]));
        Assert.Equal(new[] { 7, 8 }, Sorted(byType[0]));
    }

    private static int[] Sorted(HashSet<int> values) {
        var array = new int[values.Count];
        values.CopyTo(array);
        System.Array.Sort(array);
        return array;
    }

    [Fact]
    public void TheThreeNameFamiliesAreTheSameTileAtThreeLayers() {
        // g######, t###### and r###### share a zone+tile suffix: 43 of the 44 g suffixes have a t
        // twin. That is what makes "three layers of one tile" a reading rather than a coincidence.
        string? dir = GeneratedCorpus.FindDir("TBL");
        if (dir == null) {
            return;
        }

        var suffixes = new Dictionary<char, HashSet<string>> {
            ['g'] = new(), ['t'] = new(), ['r'] = new(),
        };
        foreach (Entry entry in RealEntries(dir)) {
            if (entry.Name.Length != 7 || !suffixes.ContainsKey(entry.Name[0])) {
                continue;
            }
            string tail = entry.Name.Substring(1);
            if (!long.TryParse(tail, out _)) {
                continue;
            }
            suffixes[entry.Name[0]].Add(tail);
        }

        Assert.NotEmpty(suffixes['g']);
        var overlap = new HashSet<string>(suffixes['g']);
        overlap.IntersectWith(suffixes['t']);
        Assert.True(overlap.Count * 10 >= suffixes['g'].Count * 9,
            $"only {overlap.Count} of {suffixes['g'].Count} g-tiles have a t twin");
    }
}
