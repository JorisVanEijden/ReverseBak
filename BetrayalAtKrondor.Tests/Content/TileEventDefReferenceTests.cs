namespace BetrayalAtKrondor.Tests.Content;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GameData.Resources.Content;
using Xunit;

/// <summary>Integration enforcement of reference #2 (tile-event <c>TileEventTrigger.EntryNumber</c> →
/// type-selected <c>def_&lt;Type&gt;.dat</c> record) over the committed <c>generated/</c> corpus, via
/// <see cref="ReferenceValidator"/>. Runs in <b>key mode</b>: every trigger's <c>EntryKey</c>
/// (<c>base:def_&lt;type&gt;:&lt;n&gt;</c>) must resolve to a <c>DefRecord.Key</c> in the global
/// <c>def</c> catalog — the type-selection (which DEF family) is baked into the key, so the test needs
/// no enum→file mapping of its own. Skip-if-absent. See docs/re-notes/reference-inventory.md row 2.</summary>
public class TileEventDefReferenceTests {
    [Fact]
    public void EveryTileEventTrigger_ReferencesAValidDefRecord() {
        string? gen = GeneratedCorpus.FindDir("DEF", "DAT");
        if (gen == null) {
            return;
        }

        // Catalog: every DEF record key across all def families (base:def_<family>:<index>).
        var defKeys = new HashSet<string>();
        foreach (string defPath in Directory.GetFiles(Path.Combine(gen, "DEF"), "DEF_*.json")) {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(defPath));
            foreach (JsonElement rec in doc.RootElement.GetProperty("Records").EnumerateArray()) {
                defKeys.Add(rec.GetProperty("Key").GetString()!);
            }
        }
        var catalogs = new Dictionary<string, ISet<string>> { ["def"] = defKeys };

        // References: each tile-event trigger's EntryKey → the def catalog. Tile-event tiles are the
        // DAT/T######.json files that carry a "Chapters" array (distinct from world-item tiles in WLD/).
        var refs = new List<ContentReference>();
        foreach (string datPath in Directory.GetFiles(Path.Combine(gen, "DAT"), "T??????.json")) {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(datPath));
            if (!doc.RootElement.TryGetProperty("Chapters", out JsonElement chapters)) {
                continue;
            }
            string tile = Path.GetFileNameWithoutExtension(datPath);
            int i = 0;
            foreach (JsonElement chapter in chapters.EnumerateArray()) {
                foreach (JsonElement trigger in chapter.GetProperty("Triggers").EnumerateArray()) {
                    string entryKey = trigger.GetProperty("EntryKey").GetString()!;
                    refs.Add(new ContentReference($"base:tileevent:{tile}:{i}", "def", entryKey));
                    i++;
                }
            }
        }

        IReadOnlyList<BrokenReference> broken = ReferenceValidator.Validate(catalogs, refs);
        Assert.True(broken.Count == 0,
            $"{broken.Count} tile-event triggers reference a missing DEF record. First few: " +
            string.Join("; ", broken.Take(5).Select(b => $"{b.FromKey} → {b.TargetCatalog}:{b.TargetKey}")));

        // Non-empty guard: the shipping tile-event corpus has well over a thousand triggers.
        Assert.True(refs.Count > 0, "Found no tile-event triggers — the DAT tile-event corpus should be non-empty.");
    }
}
