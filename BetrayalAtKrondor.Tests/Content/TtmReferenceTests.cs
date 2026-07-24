namespace BetrayalAtKrondor.Tests.Content;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GameData.Resources.Content;
using Xunit;

/// <summary>Integration enforcement of the TTM cross-references over the committed <c>generated/</c>
/// corpus, via <see cref="ReferenceValidator"/>:
///  #6  <c>GotoFrame.TargetKey</c> → a tagged <see cref="Frame"/> in the same TTM
///      (<c>base:ttm:&lt;file&gt;:tag:&lt;n&gt;</c>). NOTE: NextFrame is a <b>tag</b>, not a frame index
///      (the runtime matches <c>Frame.Tag</c>); the inventory's "index" label was wrong.
///  #5  <c>DialogCommand.TargetKey</c> → global DDX dialog <c>base:dialog:&lt;Dialog16Id+1600000&gt;</c>.
/// Both run in key mode. Skip-if-absent. See docs/re-notes/reference-inventory.md rows 5/6.</summary>
public class TtmReferenceTests {
    [Fact]
    public void EveryGotoFrame_ReferencesATaggedFrameInSameTtm() {
        string? gen = GeneratedCorpus.FindDir("TTM");
        if (gen == null) {
            return;
        }

        int total = 0;
        foreach (string ttmPath in Directory.GetFiles(Path.Combine(gen, "TTM"), "*.json")) {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(ttmPath));
            // Per-TTM catalog: the tagged frames' keys.
            var frameKeys = new HashSet<string>();
            var gotos = new List<ContentReference>();
            int fi = 0;
            foreach (JsonElement frame in doc.RootElement.GetProperty("Frames").EnumerateArray()) {
                if (frame.TryGetProperty("Key", out JsonElement k) && k.GetString() is string key && key.Length > 0) {
                    frameKeys.Add(key);
                }
                foreach (JsonElement cmd in frame.GetProperty("Commands").EnumerateArray()) {
                    if (cmd.TryGetProperty("$type", out JsonElement t) && t.GetString() == "GotoFrame") {
                        gotos.Add(new ContentReference($"{Path.GetFileNameWithoutExtension(ttmPath)}:frame{fi}",
                            "frame", cmd.GetProperty("TargetKey").GetString()!));
                    }
                }
                fi++;
            }
            var catalogs = new Dictionary<string, ISet<string>> { ["frame"] = frameKeys };
            IReadOnlyList<BrokenReference> broken = ReferenceValidator.Validate(catalogs, gotos);
            Assert.True(broken.Count == 0,
                $"{Path.GetFileName(ttmPath)}: {broken.Count} GotoFrame(s) target a missing tag. " +
                string.Join("; ", broken.Take(3).Select(b => $"{b.FromKey} → {b.TargetKey}")));
            total += gotos.Count;
        }
        Assert.True(total > 0, "Found no GotoFrame commands across the TTM corpus.");
    }

    [Fact]
    public void EveryTtmDialogCommand_ReferencesAValidDdxDialog() {
        string? gen = GeneratedCorpus.FindDir("TTM", "DDX");
        if (gen == null) {
            return;
        }

        // Global DDX dialog-id catalog (same id space #3/#4 use).
        var dialogKeys = new HashSet<string>();
        foreach (string ddxPath in Directory.GetFiles(Path.Combine(gen, "DDX"), "*.json")) {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(ddxPath));
            foreach (JsonElement entry in doc.RootElement.GetProperty("Entries").EnumerateArray()) {
                if (entry.TryGetProperty("Id", out JsonElement id) && id.ValueKind == JsonValueKind.Number) {
                    dialogKeys.Add(ContentKey.ForBase("dialog", id.GetInt32()));
                }
            }
        }
        var catalogs = new Dictionary<string, ISet<string>> { ["dialog"] = dialogKeys };

        var refs = new List<ContentReference>();
        foreach (string ttmPath in Directory.GetFiles(Path.Combine(gen, "TTM"), "*.json")) {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(ttmPath));
            string ttm = Path.GetFileNameWithoutExtension(ttmPath);
            int fi = 0;
            foreach (JsonElement frame in doc.RootElement.GetProperty("Frames").EnumerateArray()) {
                foreach (JsonElement cmd in frame.GetProperty("Commands").EnumerateArray()) {
                    if (cmd.TryGetProperty("$type", out JsonElement t) && t.GetString() == "DialogCommand"
                        && cmd.TryGetProperty("TargetKey", out JsonElement tk) && tk.ValueKind == JsonValueKind.String) {
                        refs.Add(new ContentReference($"{ttm}:frame{fi}", "dialog", tk.GetString()!));
                    }
                }
                fi++;
            }
        }

        IReadOnlyList<BrokenReference> broken = ReferenceValidator.Validate(catalogs, refs);

        // Known single dangling reference: C93 DialogCommand Dialog16Id=255 → dialog 1600255, which no
        // DDX entry defines (a plausible authoring stray; the runtime gates Dialog16Id>0 then looks up
        // by id, finding nothing). Baseline it so the gate still catches new dangling dialog targets.
        var unexpected = broken.Where(b => b.TargetKey != ContentKey.ForBase("dialog", 1600255)).ToList();
        Assert.True(unexpected.Count == 0,
            $"{unexpected.Count} TTM DialogCommands target a missing DDX dialog (outside the known 1600255 stray). " +
            string.Join("; ", unexpected.Take(5).Select(b => $"{b.FromKey} → {b.TargetKey}")));
        Assert.True(refs.Count > 0, "Found no dialog-showing DialogCommands across the TTM corpus.");
    }
}
