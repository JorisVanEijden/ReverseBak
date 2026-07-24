namespace BetrayalAtKrondor.Tests.Content;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GameData.Resources.Content;
using Xunit;

/// <summary>Integration enforcement of reference #7 (dialog <c>KeywordChoiceBranch.Keyword</c> →
/// KEYWORD.DAT label) over the committed <c>generated/</c> corpus, via <see cref="ReferenceValidator"/>.
/// The branch's <c>Keyword</c> is a <b>1-based</b> menu-label index; the target is the keyword-catalog
/// entry at <c>Keyword - 1</c> (the catalog dictionary is 0-based, keys "0".."N-1"). Encoding the −1
/// here is the point: it's the off-by-one the format is prone to, and this gate holds it fixed. All
/// KeywordChoiceBranch entries live in DIAL_Z20 (the town/travel keyword menu). Skip-if-absent.
/// See docs/re-notes/reference-inventory.md row 7.</summary>
public class KeywordReferenceTests {
    [Fact]
    public void EveryKeywordChoice_ReferencesAValidKeyword() {
        string? gen = GeneratedCorpus.FindDir(Path.Combine("DAT", "keywords.json"), "DDX");
        if (gen == null) {
            return;
        }

        // Catalog: keyword ids are the (0-based) dictionary keys of DAT/keywords.json.
        var keywordKeys = new HashSet<string>();
        using (JsonDocument kw = JsonDocument.Parse(File.ReadAllText(Path.Combine(gen, "DAT", "keywords.json")))) {
            foreach (JsonProperty entry in kw.RootElement.GetProperty("Keywords").EnumerateObject()) {
                keywordKeys.Add(entry.Name);
            }
        }
        var catalogs = new Dictionary<string, ISet<string>> { ["keyword"] = keywordKeys };

        // References: every KeywordChoiceBranch across the DDX corpus → keyword at (Keyword - 1).
        var refs = new List<ContentReference>();
        foreach (string ddxPath in Directory.GetFiles(Path.Combine(gen, "DDX"), "*.json")) {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(ddxPath));
            string ddx = Path.GetFileNameWithoutExtension(ddxPath);
            int i = 0;
            foreach (int keyword in DdxActionCollector.CollectIntFieldByType(doc.RootElement, "KeywordChoiceBranch", "Keyword")) {
                refs.Add(new ContentReference($"base:ddx:{ddx}:kwchoice:{i}", "keyword", (keyword - 1).ToString()));
                i++;
            }
        }

        IReadOnlyList<BrokenReference> broken = ReferenceValidator.Validate(catalogs, refs);
        Assert.True(broken.Count == 0,
            $"{broken.Count} KeywordChoiceBranch references do not resolve (Keyword-1 outside the keyword catalog). " +
            $"First few: " +
            string.Join("; ", broken.Take(5).Select(b => $"{b.FromKey} → {b.TargetCatalog}:{b.TargetKey}")));

        // Guard against a silently-empty corpus (e.g. the branch type stops serializing its field):
        // KeywordChoiceBranch is known to be present (159 in DIAL_Z20), so zero references is a bug.
        Assert.True(refs.Count > 0, "Found no KeywordChoiceBranch references — expected 159 in DIAL_Z20.");
    }
}
