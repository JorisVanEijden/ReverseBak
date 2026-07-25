namespace BetrayalAtKrondor.Tests.Content;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GameData.Resources.Content;
using Xunit;

/// <summary>Integration enforcement of reference #15 (encounter <c>EnemySlot.CreatureNumber</c> →
/// creature) over the committed <c>generated/</c> corpus, via <see cref="ReferenceValidator"/>. Runs in
/// key mode: every DEF combat/trap enemy slot's <c>CreatureKey</c> (<c>base:mnames:&lt;n&gt;</c>) must
/// resolve to a <c>CreatureName.Key</c> in the mnames catalog (DAT/mnames.json). Empty/filler slots
/// carry CreatureNumber 0 = the game's own "INVALID MONSTER" mnames[0], which resolves faithfully.
/// Skip-if-absent. See docs/re-notes/reference-inventory.md row 15.</summary>
public class EncounterCreatureReferenceTests {
    [Fact]
    public void EveryEnemySlot_ReferencesAValidCreature() {
        string? gen = GeneratedCorpus.FindDir(Path.Combine("DAT", "mnames.json"), "DEF");
        if (gen == null) {
            return;
        }

        // Catalog: the mnames creature keys.
        var mnamesKeys = new HashSet<string>();
        using (JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(gen, "DAT", "mnames.json")))) {
            foreach (JsonElement c in doc.RootElement.GetProperty("Creatures").EnumerateArray()) {
                mnamesKeys.Add(c.GetProperty("Key").GetString()!);
            }
        }
        var catalogs = new Dictionary<string, ISet<string>> { ["mnames"] = mnamesKeys };

        var refs = new List<ContentReference>();
        foreach (string defName in new[] { "DEF_COMB.json", "DEF_TRAP.json" }) {
            string path = Path.Combine(gen, "DEF", defName);
            if (!File.Exists(path)) {
                continue;
            }
            AddCreatureRefs(refs, path, Path.GetFileNameWithoutExtension(defName).ToLowerInvariant());
        }

        IReadOnlyList<BrokenReference> broken = ReferenceValidator.Validate(catalogs, refs);
        Assert.True(broken.Count == 0,
            $"{broken.Count} enemy slots reference a missing creature. First few: " +
            string.Join("; ", broken.Take(5).Select(b => $"{b.FromKey} → {b.TargetCatalog}:{b.TargetKey}")));

        Assert.True(refs.Count > 0, "Found no enemy slots — the DEF combat/trap corpus should be non-empty.");
    }

    private static void AddCreatureRefs(List<ContentReference> refs, string path, string fromPrefix) {
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
        int r = 0;
        foreach (JsonElement rec in doc.RootElement.GetProperty("Records").EnumerateArray()) {
            if (rec.TryGetProperty("Payload", out JsonElement payload)
                && payload.ValueKind == JsonValueKind.Object
                && payload.TryGetProperty("EnemySetup", out JsonElement setup)
                && setup.TryGetProperty("Slots", out JsonElement slots)) {
                int s = 0;
                foreach (JsonElement slot in slots.EnumerateArray()) {
                    if (slot.TryGetProperty("CreatureKey", out JsonElement ck) && ck.ValueKind == JsonValueKind.String) {
                        refs.Add(new ContentReference($"base:{fromPrefix}:{r}:slot{s}", "mnames", ck.GetString()!));
                    }
                    s++;
                }
            }
            r++;
        }
    }
}
