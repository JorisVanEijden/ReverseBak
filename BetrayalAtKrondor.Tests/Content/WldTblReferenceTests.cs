namespace BetrayalAtKrondor.Tests.Content;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GameData.Resources.Content;
using Xunit;

/// <summary>Integration enforcement of reference #1 (WLD <c>TypeId</c> → zone TBL entity) over the
/// committed <c>generated/</c> corpus, via <see cref="ReferenceValidator"/>. As of the de-index shape
/// change this runs in <b>key mode</b>: every <c>WorldItem.EntityKey</c> (<c>base:tbl:z&lt;zone&gt;:&lt;id&gt;</c>)
/// must resolve to a <c>ZoneTableEntry.Key</c> in the global <c>tbl</c> catalog — no index arithmetic,
/// no zone-mapping assumption in the test (both sides emit the same stable string). Skip-if-absent.
/// See docs/re-notes/reference-inventory.md row 1.</summary>
public class WldTblReferenceTests {
    [Fact]
    public void EveryWorldItem_ReferencesAValidZoneTblEntity() {
        string? gen = GeneratedCorpus.FindDir("WLD", "TBL");
        if (gen == null) {
            return; // generated/ not present (e.g. CI without game data) — skip, don't fail.
        }

        // Catalog: the set of every TBL entity key across all zone tables (base:tbl:<table>:<index>).
        var tblKeys = new HashSet<string>();
        foreach (string tblPath in Directory.GetFiles(Path.Combine(gen, "TBL"), "Z*.json")) {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(tblPath));
            foreach (JsonElement entry in doc.RootElement.GetProperty("Entries").EnumerateArray()) {
                tblKeys.Add(entry.GetProperty("Key").GetString()!);
            }
        }
        var catalogs = new Dictionary<string, ISet<string>> { ["tbl"] = tblKeys };

        // References: each WorldItem's EntityKey → the tbl catalog.
        var refs = new List<ContentReference>();
        foreach (string wldPath in Directory.GetFiles(Path.Combine(gen, "WLD"), "T*.json")) {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(wldPath));
            string tile = Path.GetFileNameWithoutExtension(wldPath);
            int idx = 0;
            foreach (JsonElement item in doc.RootElement.GetProperty("Items").EnumerateArray()) {
                string entityKey = item.GetProperty("EntityKey").GetString()!;
                refs.Add(new ContentReference($"base:wld:{tile}:{idx}", "tbl", entityKey));
                idx++;
            }
        }

        IReadOnlyList<BrokenReference> broken = ReferenceValidator.Validate(catalogs, refs);

        // Known pre-existing extraction anomaly: WorldItemExtractor mis-parses a mid-file section of
        // tile T091011 into ~7 garbage records with impossible (>=1000) TypeIds — a WLD-format bug
        // logged in docs/work-todo.md ("WLD extraction anomaly — T091011"). Their EntityKeys
        // (base:tbl:z09:<garbage>) don't resolve. Baseline that cluster so this gate catches genuine
        // linking regressions (a plausible-magnitude id that stops resolving) while tolerating the
        // documented garbage. Every unresolved reference must be one of those records.
        var unexpected = broken
            .Where(b => !(b.FromKey.StartsWith("base:wld:T091011:", StringComparison.Ordinal)
                          && ParsedIndex(b.TargetKey) >= 1000))
            .ToList();

        Assert.True(unexpected.Count == 0,
            $"{unexpected.Count} unexpected WorldItem→TBL references do not resolve " +
            $"(outside the known T091011 garbage cluster). First few: " +
            string.Join("; ", unexpected.Take(5).Select(b => $"{b.FromKey} → {b.TargetCatalog}:{b.TargetKey}")));

        // Non-empty guard: the corpus has tens of thousands of world items.
        Assert.True(refs.Count > 0, "Found no WorldItem references — the WLD corpus should be non-empty.");
    }

    /// <summary>Parses the trailing index from a <c>base:tbl:z&lt;zone&gt;:&lt;index&gt;</c> key
    /// (−1 if malformed), used only to classify the T091011 garbage cluster.</summary>
    private static int ParsedIndex(string key) {
        int colon = key.LastIndexOf(':');
        return colon >= 0 && int.TryParse(key.AsSpan(colon + 1), out int i) ? i : -1;
    }
}
