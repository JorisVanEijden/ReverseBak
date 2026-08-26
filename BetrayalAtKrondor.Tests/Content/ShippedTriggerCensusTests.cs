namespace BetrayalAtKrondor.Tests.Content;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

/// <summary>
/// Which hotspot kinds the shipped game actually uses.
/// </summary>
/// <remarks>
/// <b>The DEF family table has twelve entries and the game ships nine files.</b>
/// <c>g_aDefFileNames</c> (canassa HOTSPOT.C:39) names <c>def_comm.dat</c>, <c>def_heal.dat</c> and
/// <c>def_soun.dat</c> alongside the nine that exist — none of the three is on disk or in
/// KRONDOR.001. Reading the table as the list of kinds to port would put three arms on the backlog
/// that no data can ever reach.
///
/// <para>This asserts the other half of that: not only is there no <c>def_soun.dat</c>, there is no
/// <c>Soun</c> TRIGGER either, in any of the shipped tile files. The two facts together are what
/// make it dead rather than merely unloadable.</para>
/// </remarks>
public class ShippedTriggerCensusTests {
    // Kinds with at least one trigger somewhere in the shipped data.
    private static readonly string[] Used = {
        "Comb", "Trap", "Dial", "Zone", "Bloc", "Town", "Disa", "Enab", "Bkgr",
    };

    private static Dictionary<string, int> Census(string dir) {
        var counts = new Dictionary<string, int>();
        foreach (string path in Directory.EnumerateFiles(dir, "T*.json")) {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("Chapters", out JsonElement chapters)) {
                continue;
            }
            foreach (JsonElement chapter in chapters.EnumerateArray()) {
                if (!chapter.TryGetProperty("Triggers", out JsonElement triggers)) {
                    continue;
                }
                foreach (JsonElement trigger in triggers.EnumerateArray()) {
                    string? type = trigger.TryGetProperty("Type", out JsonElement t) ? t.GetString() : null;
                    if (type == null) {
                        continue;
                    }
                    counts[type] = counts.TryGetValue(type, out int n) ? n + 1 : 1;
                }
            }
        }
        return counts;
    }

    // The T-files live in generated/DAT alongside a lot else, so require a couple of siblings that
    // pin the right directory rather than trusting the first "generated" up the tree.
    private static string? TileDir() {
        string? root = GeneratedCorpus.FindDir("DAT", "DEF");
        if (root == null) {
            return null;
        }
        string dat = Path.Combine(root, "DAT");
        return Directory.EnumerateFiles(dat, "T*.json").Any() ? dat : null;
    }

    [Fact]
    public void NoSounTriggerShipsAnywhere() {
        string? dir = TileDir();
        if (dir == null) {
            return;   // no corpus on this machine
        }

        Dictionary<string, int> counts = Census(dir);

        // Guard against a silently empty census, which would pass this test for the wrong reason.
        Assert.True(counts.Values.Sum() > 1000, $"census looks empty: {counts.Values.Sum()} triggers");

        Assert.False(counts.ContainsKey("Soun"));
        Assert.False(counts.ContainsKey("Comm"));
        Assert.False(counts.ContainsKey("Heal"));
    }

    [Fact]
    public void TrapIsTheSecondMostCommonKind_NotAnAfterthoughtBesideSoun() {
        // The dispatcher used to file "Soun / Trap" together as one unported bucket. One of them can
        // never fire and the other is the second-busiest trigger in the game.
        string? dir = TileDir();
        if (dir == null) {
            return;
        }

        Dictionary<string, int> counts = Census(dir);
        List<KeyValuePair<string, int>> ranked = counts.OrderByDescending(p => p.Value).ToList();

        Assert.Equal("Comb", ranked[0].Key);
        Assert.Equal("Trap", ranked[1].Key);
        Assert.True(ranked[1].Value > 300, $"Trap ships {ranked[1].Value} triggers");
    }

    [Fact]
    public void EveryKindThatShipsIsOneWeKnowAbout() {
        // A kind appearing here that is not in the list is a whole feature nobody has looked at.
        string? dir = TileDir();
        if (dir == null) {
            return;
        }

        foreach (string kind in Census(dir).Keys) {
            Assert.Contains(kind, Used);
        }
    }
}
