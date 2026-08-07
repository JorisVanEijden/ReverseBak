namespace BetrayalAtKrondor.Tests.Text;

using BetrayalAtKrondor.Tests.Content;
using ResourceExtraction.Extractors.Exe;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

/// <summary>
/// The completeness gate. The candidate list is IDA-derived (a C# test cannot disassemble), so it is
/// committed as a reviewed baseline and this test enforces the manifest against it — the same shape
/// as the reference-integrity tests over docs/re-notes/reference-inventory.md.
/// Skips when the docs tree is absent, matching the corpus tests' skip-if-absent contract.
///
/// <para><b>Occurrences, not presence.</b> The same text appears at several addresses (the 1993
/// compiler pooled nothing) and each address is a separate call site needing its own key. Comparing
/// SETS of text let a regeneration that turned up a fifth <c>Accuracy:</c> pass against four
/// declarations — the gate would stay green while a call site went unkeyed. Both directions
/// therefore compare counts per text.</para>
/// </summary>
public class ExeStringCoverageTests {
    private static string? FindBaseline() {
        DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null) {
            string p = Path.Combine(dir.FullName, "docs", "re-notes", "exe-display-strings.md");
            if (File.Exists(p)) {
                return p;
            }
            dir = dir.Parent;
        }
        return null;
    }

    // Rows look like: | `0x3abb8` | `Party Gold:` |   (candidates)
    //             or: | `text` | reason |               (the other two tables)
    //
    // THREE sections, not two. The third — "Declared beyond the classifier's reach" — holds genuine
    // display strings the generator provably cannot find: the classifier is function-level, so text
    // formatted by a helper one call away from the draw site (the money wordings in
    // FormatMoneyToString, shopkeeper/tavernkeeper in PopulateDialogSlotText) is invisible to it.
    // Those are declared deliberately, so they satisfy the reverse-direction check exactly as an
    // exclusion does — but they are NOT exclusions, and conflating the two would hide the
    // distinction between "we chose to skip this" and "the tool cannot see this".
    //
    // Candidates and out-of-reach rows are both COUNTED, one row per call site — an out-of-reach row
    // licenses one declaration, exactly as a candidate row does, so a text that occurs in both
    // tables (the gold/silver wordings: once in UI_DrawInventory where the classifier sees it, once
    // in FormatMoneyToString where it cannot) still has every occurrence accounted for. Making that
    // table an exempt SET instead would switch counting off for those texts, which is the hole this
    // gate was just fixed to close.
    //
    // Exclusions ARE an exempt set, and deliberately so: an exclusion says "this text is not keyed
    // anywhere, at any address" (format glue, a fopen mode, a cheat menu), so counting its
    // occurrences would be counting something nobody intends to declare.
    internal static (Dictionary<string, int> candidates, Dictionary<string, int> outOfReach,
        HashSet<string> excluded) Parse(string path) {
        var candidates = new Dictionary<string, int>(StringComparer.Ordinal);
        var outOfReach = new Dictionary<string, int>(StringComparer.Ordinal);
        var excluded = new HashSet<string>(StringComparer.Ordinal);
        string section = "";
        foreach (string line in File.ReadAllLines(path)) {
            if (line.StartsWith("## ", StringComparison.Ordinal)) {
                section = line.Substring(3).Trim();
                continue;
            }
            if (!line.StartsWith("|", StringComparison.Ordinal)) {
                continue;
            }
            string[] cells = line.Split('|');
            if (cells.Length < 3) {
                continue;
            }
            // Extract backtick-delimited text. The baseline uses the convention `text` (0xADDRESS)
            // for some rows (Exclusions with address suffixes), where Trim('`') would leave the
            // closing backtick and address behind. Regex extracts only the backtick-delimited span,
            // ignoring any address suffix. Fall back to Trim() if no backticks are present.
            string first = ExtractBacktickContent(cells[1].Trim());
            string second = ExtractBacktickContent(cells[2].Trim());
            if (first is "address" or "text" or "---") {
                continue;
            }
            if (section == "Candidates") {
                candidates.TryGetValue(second, out int n);
                candidates[second] = n + 1;
            } else if (section == "Exclusions") {
                excluded.Add(first);
            } else if (section.StartsWith("Declared beyond", StringComparison.Ordinal)) {
                outOfReach.TryGetValue(first, out int n);
                outOfReach[first] = n + 1;
            }
        }
        return (candidates, outOfReach, excluded);
    }

    private static string ExtractBacktickContent(string cell) {
        var match = Regex.Match(cell, @"`([^`]*)`");
        return match.Success ? match.Groups[1].Value : cell;
    }

    private static Dictionary<string, int> DeclaredCounts() {
        var declared = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (ExeStringSingle s in ExeStringManifest.Singles) {
            declared.TryGetValue(s.Text, out int n);
            declared[s.Text] = n + 1;
        }
        return declared;
    }

    [Fact]
    public void EveryBaselineCandidateOccurrenceIsDeclared() {
        string? path = FindBaseline();
        if (path == null) {
            return; // docs tree absent (CI without the repo root)
        }
        (Dictionary<string, int> candidates, _, HashSet<string> excluded) = Parse(path);
        Dictionary<string, int> declared = DeclaredCounts();
        var missing = new List<string>();
        foreach (KeyValuePair<string, int> c in candidates) {
            if (excluded.Contains(c.Key)) {
                continue; // deliberately never keyed, at any address
            }
            declared.TryGetValue(c.Key, out int have);
            if (have < c.Value) {
                missing.Add($"\"{c.Key}\" baselined {c.Value}x, declared {have}x");
            }
        }
        Assert.True(missing.Count == 0,
            "Undeclared display-string occurrences (add a key per call site to " +
            "ExeStringManifest.Singles, or add the text to the Exclusions table with a reason): "
            + string.Join(" | ", missing));
    }

    [Fact]
    public void EveryDeclaredSingleOccurrenceIsBaselinedOrExcluded() {
        string? path = FindBaseline();
        if (path == null) {
            return;
        }
        (Dictionary<string, int> candidates, Dictionary<string, int> outOfReach,
            HashSet<string> excluded) = Parse(path);
        var stray = new List<string>();
        foreach (KeyValuePair<string, int> d in DeclaredCounts()) {
            if (excluded.Contains(d.Key)) {
                continue;
            }
            candidates.TryGetValue(d.Key, out int baselined);
            outOfReach.TryGetValue(d.Key, out int reachable);
            if (d.Value > baselined + reachable) {
                stray.Add($"\"{d.Key}\" declared {d.Value}x, accounted for {baselined + reachable}x "
                    + $"({baselined} baselined + {reachable} out-of-reach)");
            }
        }
        Assert.True(stray.Count == 0,
            "Declared occurrences the baseline does not account for. Every declaration needs one "
            + "row: a Candidates row if the classifier found it, or a 'Declared beyond the "
            + "classifier's reach' row naming why it could not. Surplus: " + string.Join(" | ", stray));
    }

    // The shipped catalog must actually contain every declared key with non-empty text.
    [Fact]
    public void CommittedCatalogCoversEveryDeclaration() {
        string? generated = GeneratedCorpus.FindDir(Path.Combine("EXE", "uistrings.json"));
        if (generated == null) {
            return;
        }
        string json = File.ReadAllText(Path.Combine(generated, "EXE", "uistrings.json"));
        Dictionary<string, string> catalog =
            JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? new Dictionary<string, string>();
        foreach (ExeStringTable t in ExeStringManifest.Tables) {
            foreach (string n in t.Names) {
                string key = $"base:uistring:{t.KeyPrefix}.{n}";
                Assert.True(catalog.ContainsKey(key), $"catalog missing {key}");
                Assert.False(string.IsNullOrEmpty(catalog[key]), $"catalog has empty {key}");
            }
        }
        foreach (ExeStringSingle s in ExeStringManifest.Singles) {
            Assert.True(catalog.ContainsKey(s.Key), $"catalog missing {s.Key}");
            Assert.False(string.IsNullOrEmpty(catalog[s.Key]), $"catalog has empty {s.Key}");
        }
    }

    // Regression test: backtick-quoted cells with address suffixes are parsed correctly.
    // The Exclusions table uses the convention `text` (0xADDRESS) to annotate where format
    // strings were found; naive Trim('`') leaves the closing backtick and suffix behind.
    [Fact]
    public void BacktickExtractorHandlesAddressSuffixes() {
        string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".md");
        try {
            string markdown = @"## Candidates
| address | text |
|---|---|
| `0x1234` | `Hello World` |
| `0x5678` | `%Fs` |

## Exclusions
| text | reason |
|---|---|
| `%Fs` (0x3a904) | pure format glue — no translatable words |
| `Plain text` | some reason |
";
            File.WriteAllText(tempPath, markdown);
            (Dictionary<string, int> candidates, _, HashSet<string> excluded) = Parse(tempPath);

            // Candidates table: second column must be extracted correctly.
            Assert.Contains("Hello World", candidates.Keys);
            Assert.Contains("%Fs", candidates.Keys);

            // Exclusions table: first column with and without address suffix must both yield bare text.
            Assert.Contains("%Fs", excluded);
            Assert.Contains("Plain text", excluded);

            // Verify no mangled versions are present.
            Assert.DoesNotContain("%Fs` (0x3a904)", excluded);
            Assert.DoesNotContain("Hello World`", candidates.Keys);
        } finally {
            if (File.Exists(tempPath)) {
                File.Delete(tempPath);
            }
        }
    }

    // The gate's own regression test: repeated candidate rows must be COUNTED. Before this, both
    // directions compared sets, so a baseline with two `Accuracy:` rows was satisfied by one
    // declaration — the exact hole a regeneration turning up an extra call site would fall into.
    [Fact]
    public void RepeatedCandidateRowsAreCountedNotCollapsed() {
        string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".md");
        try {
            File.WriteAllText(tempPath, @"## Candidates
| address | text |
|---|---|
| `0x1` | `Accuracy:` |
| `0x2` | `Accuracy:` |
| `0x3` | `Accuracy:` |
| `0x4` | `Once` |
");
            (Dictionary<string, int> candidates, Dictionary<string, int> outOfReach,
                HashSet<string> excluded) = Parse(tempPath);
            Assert.Equal(3, candidates["Accuracy:"]);
            Assert.Equal(1, candidates["Once"]);
            Assert.Empty(outOfReach);
            Assert.Empty(excluded);
        } finally {
            if (File.Exists(tempPath)) {
                File.Delete(tempPath);
            }
        }
    }
}
