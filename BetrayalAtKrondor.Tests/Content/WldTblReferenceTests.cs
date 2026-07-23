namespace BetrayalAtKrondor.Tests.Content;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GameData.Resources.Content;
using Xunit;

/// <summary>Integration enforcement of reference #1 (WLD <c>TypeId</c> → zone TBL entity) over the
/// committed <c>generated/</c> corpus, via <see cref="ReferenceValidator"/>. Runs in "index
/// in-bounds" mode today (target keys are the entity indices); after the extractor de-indexes
/// <c>TypeId</c>→key, the same check enforces key resolution. Skip-if-absent, like the game-data
/// tests. See docs/re-notes/reference-inventory.md row 1.</summary>
public class WldTblReferenceTests {
    private static string? FindGeneratedDir() {
        DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null) {
            string candidate = Path.Combine(dir.FullName, "generated");
            if (Directory.Exists(Path.Combine(candidate, "WLD")) &&
                Directory.Exists(Path.Combine(candidate, "TBL"))) {
                return candidate;
            }
            dir = dir.Parent;
        }
        return null;
    }

    [Fact]
    public void EveryWorldItem_ReferencesAValidZoneTblEntity() {
        string? gen = FindGeneratedDir();
        if (gen == null) {
            return; // generated/ not present (e.g. CI without game data) — skip, don't fail.
        }

        // Catalogs: tbl:z01 → { "0" .. "<entryCount-1>" } (index-as-key today).
        var catalogs = new Dictionary<string, ISet<string>>();
        foreach (string tblPath in Directory.GetFiles(Path.Combine(gen, "TBL"), "Z*.json")) {
            string name = Path.GetFileNameWithoutExtension(tblPath).ToLowerInvariant(); // z01, z10m, …
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(tblPath));
            int count = doc.RootElement.GetProperty("Entries").GetArrayLength();
            var keys = new HashSet<string>();
            for (int i = 0; i < count; i++) {
                keys.Add(i.ToString());
            }
            catalogs["tbl:" + name] = keys;
        }

        // References: each WorldItem.TypeId → its zone's TBL catalog.
        var refs = new List<ContentReference>();
        foreach (string wldPath in Directory.GetFiles(Path.Combine(gen, "WLD"), "T*.json")) {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(wldPath));
            JsonElement root = doc.RootElement;
            int zone = root.GetProperty("ZoneNumber").GetInt32();
            string cat = $"tbl:z{zone:D2}";
            string tile = Path.GetFileNameWithoutExtension(wldPath);
            int idx = 0;
            foreach (JsonElement item in root.GetProperty("Items").EnumerateArray()) {
                int typeId = item.GetProperty("TypeId").GetInt32();
                refs.Add(new ContentReference($"base:wld:{tile}:{idx}", cat, typeId.ToString()));
                idx++;
            }
        }

        IReadOnlyList<BrokenReference> broken = ReferenceValidator.Validate(catalogs, refs);

        // Known pre-existing extraction anomaly: WorldItemExtractor mis-parses a mid-file section of
        // tile T091011 into ~7 garbage records with impossible (>=1000) TypeIds — a WLD-format bug
        // logged in docs/work-todo.md ("WLD extraction anomaly — T091011"). Baseline it so this gate
        // catches genuine linking regressions (a real TypeId just past a zone's entry count) while
        // tolerating that documented cluster. Every unresolved reference must be one of those garbage
        // records; anything else — a plausible-magnitude index that doesn't resolve, or breakage in
        // another tile — fails the test.
        var unexpected = broken
            .Where(b => !(b.FromKey.StartsWith("base:wld:T091011:", StringComparison.Ordinal)
                          && int.Parse(b.TargetKey) >= 1000))
            .ToList();

        Assert.True(unexpected.Count == 0,
            $"{unexpected.Count} unexpected WorldItem→TBL references do not resolve " +
            $"(outside the known T091011 garbage cluster). First few: " +
            string.Join("; ", unexpected.Take(5).Select(b => $"{b.FromKey} → {b.TargetCatalog}:{b.TargetKey}")));
    }
}
