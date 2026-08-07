namespace BetrayalAtKrondor.Tests.Text;

using BetrayalAtKrondor.Tests.Content;
using GameData.Resources.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Xunit;

/// <summary>
/// The catalog exists in three committed places: <c>generated/EXE/uistrings.json</c> (the extractor's
/// output, what <c>verify-generated</c> diffs), <c>DotNetProjects/GameData/Content/uistrings.json</c>
/// (the embedded resource's source), and the bytes inside the shipped <c>GameData.dll</c>. Two manual
/// steps join them — a file copy and a plugin rebuild — and nothing asserted they agreed. This branch
/// alone carried two catch-up commits for precisely that drift.
///
/// <para>This test closes the loop end to end: it compares what a consumer actually reads
/// (<see cref="UiStringCatalog.Embedded"/>, i.e. the bytes compiled into the assembly under test)
/// against the extractor's committed output. A missed copy or a stale build fails here instead of
/// shipping a UI whose labels quietly predate the last extraction.</para>
///
/// <para>Skips when the <c>generated/</c> tree is absent, matching the corpus tests' skip-if-absent
/// contract (a checkout without the game-data outputs is a valid CI configuration).</para>
/// </summary>
public class EmbeddedCatalogMatchesGeneratedTests {
    [Fact]
    public void EmbeddedCatalogEqualsTheCommittedGeneratedJson() {
        string? generated = GeneratedCorpus.FindDir(Path.Combine("EXE", "uistrings.json"));
        if (generated == null) {
            return;
        }
        string json = File.ReadAllText(Path.Combine(generated, "EXE", "uistrings.json"));
        Dictionary<string, string> onDisk =
            JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? new Dictionary<string, string>();
        IReadOnlyDictionary<string, string> embedded = UiStringCatalog.Embedded.Entries;

        // Report every difference at once, keyed, rather than failing on the first: a drift is
        // normally a whole regeneration's worth of changes, and one key at a time would take as
        // many runs as there are differences to understand what happened.
        var problems = new List<string>();
        foreach (KeyValuePair<string, string> kv in onDisk) {
            if (!embedded.TryGetValue(kv.Key, out string? mine)) {
                problems.Add($"{kv.Key}: in generated/, missing from the embedded catalog");
            } else if (!string.Equals(mine, kv.Value, StringComparison.Ordinal)) {
                problems.Add($"{kv.Key}: embedded \"{mine}\" != generated \"{kv.Value}\"");
            }
        }
        foreach (KeyValuePair<string, string> kv in embedded) {
            if (!onDisk.ContainsKey(kv.Key)) {
                problems.Add($"{kv.Key}: in the embedded catalog, missing from generated/");
            }
        }

        Assert.True(problems.Count == 0,
            "The embedded UI string catalog and generated/EXE/uistrings.json disagree. Re-run "
            + "`dotnet run --project DotNetProjects/ResourceExtractor -- --uistrings <gamepath>`, "
            + "copy its output to DotNetProjects/GameData/Content/uistrings.json, and rebuild the "
            + "Unity plugin DLLs (`dotnet build -c Unity`). Differences ("
            + problems.Count + "): " + string.Join(" | ", problems));
    }

    // A guard on the guard: if the embedded catalog were empty (a dropped <EmbeddedResource> entry,
    // or Embedded silently taking its name==null fallback), the comparison above would still fail —
    // but only by listing every key, which reads like a content drift rather than a build fault.
    // Naming the case makes the diagnosis obvious.
    [Fact]
    public void EmbeddedCatalogIsNotEmpty() =>
        Assert.NotEmpty(UiStringCatalog.Embedded.Entries);
}
