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
/// </summary>
public class ExeStringCoverageTests {
    private static string FindBaseline() {
        DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
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
    internal static (HashSet<string> candidates, HashSet<string> allowed) Parse(string path) {
        var candidates = new HashSet<string>(StringComparer.Ordinal);
        var allowed = new HashSet<string>(StringComparer.Ordinal);
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
                candidates.Add(second);
            } else if (section == "Exclusions" || section.StartsWith("Declared beyond", StringComparison.Ordinal)) {
                // Both tables key on the text in column 1 and satisfy the reverse-direction check.
                allowed.Add(first);
            }
        }
        return (candidates, allowed);
    }

    private static string ExtractBacktickContent(string cell) {
        var match = Regex.Match(cell, @"`([^`]*)`");
        return match.Success ? match.Groups[1].Value : cell;
    }

    [Fact]
    public void EveryBaselineCandidateIsDeclared() {
        string path = FindBaseline();
        if (path == null) {
            return; // docs tree absent (CI without the repo root)
        }
        (HashSet<string> candidates, HashSet<string> allowed) = Parse(path);
        var declared = new HashSet<string>(StringComparer.Ordinal);
        foreach (ExeStringSingle s in ExeStringManifest.Singles) {
            declared.Add(s.Text);
        }
        var missing = new List<string>();
        foreach (string c in candidates) {
            if (!declared.Contains(c) && !allowed.Contains(c)) {
                missing.Add(c);
            }
        }
        Assert.True(missing.Count == 0,
            "Undeclared display strings (add to ExeStringManifest.Singles, or to the Exclusions " +
            "table with a reason): " + string.Join(" | ", missing));
    }

    [Fact]
    public void EveryDeclaredSingleIsBaselinedOrExcluded() {
        string path = FindBaseline();
        if (path == null) {
            return;
        }
        (HashSet<string> candidates, HashSet<string> allowed) = Parse(path);
        var stray = new List<string>();
        foreach (ExeStringSingle s in ExeStringManifest.Singles) {
            if (!candidates.Contains(s.Text) && !allowed.Contains(s.Text)) {
                stray.Add(s.Text);
            }
        }
        Assert.True(stray.Count == 0,
            "Declared strings absent from the baseline — regenerate it or remove them: "
            + string.Join(" | ", stray));
    }

    // The shipped catalog must actually contain every declared key with non-empty text.
    [Fact]
    public void CommittedCatalogCoversEveryDeclaration() {
        string generated = GeneratedCorpus.FindDir(Path.Combine("EXE", "uistrings.json"));
        if (generated == null) {
            return;
        }
        string json = File.ReadAllText(Path.Combine(generated, "EXE", "uistrings.json"));
        Dictionary<string, string> catalog =
            JsonSerializer.Deserialize<Dictionary<string, string>>(json);
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
            (HashSet<string> candidates, HashSet<string> allowed) = Parse(tempPath);

            // Candidates table: second column must be extracted correctly.
            Assert.Contains("Hello World", candidates);
            Assert.Contains("%Fs", candidates);

            // Exclusions table: first column with and without address suffix must both yield bare text.
            Assert.Contains("%Fs", allowed);
            Assert.Contains("Plain text", allowed);

            // Verify no mangled versions are present.
            Assert.DoesNotContain("%Fs` (0x3a904)", allowed);
            Assert.DoesNotContain("Hello World`", candidates);
        } finally {
            if (File.Exists(tempPath)) {
                File.Delete(tempPath);
            }
        }
    }
}
