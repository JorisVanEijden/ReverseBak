namespace BetrayalAtKrondor.Tests.Content;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GameData.Resources.Content;
using Xunit;

/// <summary>Integration enforcement of the dialog <c>Teleport.DestinationId</c> → TELEPORT.DAT
/// destination reference (reference-inventory §B) over the committed <c>generated/</c> corpus, via
/// <see cref="ReferenceValidator"/>. Teleport destinations are id'd 0..N by position; every dialog
/// Teleport action must name a live destination. Skip-if-absent. This reference only became checkable
/// once TELEPORT.DAT was extracted to <c>DAT/teleport.json</c> (see the output-coverage audit).</summary>
public class TeleportReferenceTests {
    [Fact]
    public void EveryTeleportAction_ReferencesAValidDestination() {
        string? gen = GeneratedCorpus.FindDir(Path.Combine("DAT", "teleport.json"), "DDX");
        if (gen == null) {
            return;
        }

        // Catalog: teleport destination ids (0-based, from the array's Id field).
        var destKeys = new HashSet<string>();
        using (JsonDocument dest = JsonDocument.Parse(File.ReadAllText(Path.Combine(gen, "DAT", "teleport.json")))) {
            foreach (JsonElement d in dest.RootElement.EnumerateArray()) {
                destKeys.Add(d.GetProperty("Id").GetInt32().ToString());
            }
        }
        var catalogs = new Dictionary<string, ISet<string>> { ["teleport"] = destKeys };

        // References: every dialog Teleport action ($type "Teleport") → destination DestinationId.
        var refs = new List<ContentReference>();
        foreach (string ddxPath in Directory.GetFiles(Path.Combine(gen, "DDX"), "*.json")) {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(ddxPath));
            string ddx = Path.GetFileNameWithoutExtension(ddxPath);
            int i = 0;
            foreach (int destinationId in DdxActionCollector.CollectIntFieldByType(doc.RootElement, "Teleport", "DestinationId")) {
                refs.Add(new ContentReference($"base:ddx:{ddx}:teleport:{i}", "teleport", destinationId.ToString()));
                i++;
            }
        }

        IReadOnlyList<BrokenReference> broken = ReferenceValidator.Validate(catalogs, refs);
        Assert.True(broken.Count == 0,
            $"{broken.Count} Teleport actions reference a missing destination. First few: " +
            string.Join("; ", broken.Take(5).Select(b => $"{b.FromKey} → {b.TargetCatalog}:{b.TargetKey}")));

        // Non-empty guard: 34 Teleport actions are known present across the DDX corpus.
        Assert.True(refs.Count > 0, "Found no Teleport actions — expected 34 across the DDX corpus.");
    }
}
