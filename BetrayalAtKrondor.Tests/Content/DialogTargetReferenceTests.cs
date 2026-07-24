namespace BetrayalAtKrondor.Tests.Content;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GameData.Resources.Content;
using Xunit;

/// <summary>Integration enforcement of references #3 (<c>DialogBranch.TargetOffset</c>) and #4
/// (<c>PushDialogEntry.Offset</c>) → target <c>DialogEntry</c>, over the committed <c>generated/</c>
/// corpus, via <see cref="ReferenceValidator"/>. Runs in <b>key mode</b>: every branch/push
/// <c>TargetKey</c> must resolve in the combined <c>dialog</c> catalog — the union of every entry's
/// offset key (<c>base:ddx:&lt;file&gt;:&lt;offset&gt;</c>) and every id-bearing entry's global key
/// (<c>base:dialog:&lt;id&gt;</c>). The sentinel-0 "no continuation" target emits no key and is
/// skipped. Skip-if-absent. See docs/re-notes/reference-inventory.md rows 3/4.</summary>
public class DialogTargetReferenceTests {
    [Fact]
    public void EveryDialogTarget_ReferencesAValidEntry() {
        string? gen = GeneratedCorpus.FindDir("DDX");
        if (gen == null) {
            return;
        }

        // Combined catalog: entry offset keys (all entries) ∪ global id keys (id-bearing entries).
        var dialogKeys = new HashSet<string>();
        var refs = new List<ContentReference>();
        foreach (string ddxPath in Directory.GetFiles(Path.Combine(gen, "DDX"), "*.json")) {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(ddxPath));
            string ddx = Path.GetFileNameWithoutExtension(ddxPath);
            foreach (JsonElement entry in doc.RootElement.GetProperty("Entries").EnumerateArray()) {
                dialogKeys.Add(entry.GetProperty("Key").GetString()!);
                if (entry.TryGetProperty("Id", out JsonElement id) && id.ValueKind == JsonValueKind.Number) {
                    dialogKeys.Add(ContentKey.ForBase("dialog", id.GetInt32()));
                }

                int bi = 0;
                foreach (JsonElement branch in entry.GetProperty("Branches").EnumerateArray()) {
                    AddRef(refs, branch, $"base:ddx:{ddx}:branch:{bi++}");
                }
                int ai = 0;
                foreach (JsonElement action in entry.GetProperty("Actions").EnumerateArray()) {
                    if (action.TryGetProperty("$type", out JsonElement t) && t.GetString() == "PushDialogEntry") {
                        AddRef(refs, action, $"base:ddx:{ddx}:push:{ai}");
                    }
                    ai++;
                }
            }
        }
        var catalogs = new Dictionary<string, ISet<string>> { ["dialog"] = dialogKeys };

        IReadOnlyList<BrokenReference> broken = ReferenceValidator.Validate(catalogs, refs);

        // Known single dangling authoring reference: exactly one branch (DIAL_Z19) targets global
        // dialog id 3100305, which no entry defines anywhere in the corpus (max real id is 3100369, so
        // this is a plausible authoring typo in the original data). The runtime walker treats an
        // unresolvable target as a dead end, matching the original engine. Baseline this single ref so
        // the gate still catches any *new* dangling target (verified: 1 broken of 8477 references).
        var unexpected = broken.Where(b => b.TargetKey != ContentKey.ForBase("dialog", 3100305)).ToList();

        Assert.True(unexpected.Count == 0,
            $"{unexpected.Count} dialog targets do not resolve (outside the known id-3100305 dangling ref). " +
            $"First few: " +
            string.Join("; ", unexpected.Take(5).Select(b => $"{b.FromKey} → {b.TargetCatalog}:{b.TargetKey}")));

        Assert.True(refs.Count > 0, "Found no dialog targets — the DDX corpus should be non-empty.");
    }

    private static void AddRef(List<ContentReference> refs, JsonElement node, string fromKey) {
        if (node.TryGetProperty("TargetKey", out JsonElement tk) && tk.ValueKind == JsonValueKind.String) {
            refs.Add(new ContentReference(fromKey, "dialog", tk.GetString()!));
        }
        // TargetKey null (sentinel 0 / no continuation) => no reference to check.
    }
}
