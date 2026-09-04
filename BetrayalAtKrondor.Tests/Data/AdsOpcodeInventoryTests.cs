namespace BetrayalAtKrondor.Tests.Data;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameData.Resources.Animation;
using ResourceExtraction.Extractors.Animation;
using Xunit;

/// <summary>
/// Which ADS opcodes the shipped scripts actually contain — measured over all 44 files, not inferred
/// from the engine's dispatch table.
/// </summary>
/// <remarks>
/// <b>This exists because the opposite was believed.</b> TASK-318 was filed on the reasoning that
/// <c>executeAdsCommand</c> dispatches 44 opcodes while <c>CutsceneCommand</c> names only 13, so the
/// emitted script must be dropping things — in particular <c>0x3010</c>, the weighted random branch,
/// which would make a script that varies decompile into a fixed sequence.
///
/// <para><b>Measuring it inverted the conclusion.</b> The 13 named opcodes are *exactly* the 13 that
/// occur in shipped data. The other 31 the engine can dispatch are never reached by any shipped ADS
/// file, <c>0x3010</c> among them — and its handler
/// (<c>ads_selectWeightedRandomBranch</c>, IDA 0x50F23) has exactly one xref, the dispatcher itself,
/// so no other path reaches it either. There is no lost branch and no fixed-sequence bug.</para>
///
/// <para><b>What a count of the engine's opcodes measures is the ENGINE, not the data.</b> That is
/// the reusable lesson: the ADS interpreter is general, the shipped scripts use a small corner of it,
/// and "44 opcodes exist, 13 are named" is a statement about the interpreter that says nothing about
/// whether any output is wrong. The same reasoning would condemn every other extractor whose format
/// is richer than its corpus.</para>
///
/// <para>So these tests pin the corpus rather than the engine. If shipped data ever grows an opcode
/// the script language cannot express — which is what the task feared — the first test fails with
/// its number, and the concern becomes real and actionable at that point.</para>
/// </remarks>
public class AdsOpcodeInventoryTests {
    static AdsOpcodeInventoryTests() =>
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

    /// <summary>The opcodes present across all 44 shipped ADS files, measured 2026-09-04. Every one
    /// is named by <c>CutsceneCommand.ToString()</c>, which is what keeps the emitted script free of
    /// <c>UNKNOWN_COMMAND</c>.</summary>
    private static readonly ushort[] Shipped = {
        0x1030, 0x1330, 0x1350, 0x13A0, 0x13B0, 0x1420,
        0x1500, 0x1510, 0x1520,
        0x2000, 0x2005, 0x2010,
        0xFFFF,
    };

    private static string? OriginalGameDir() {
        DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null) {
            string candidate = Path.Combine(dir.FullName, "OriginalGame");
            if (Directory.Exists(candidate) && Directory.EnumerateFiles(candidate, "*.ADS").Any()) {
                return candidate;
            }
            dir = dir.Parent;
        }
        return null;   // no game data on this machine (e.g. a cloud CI runner)
    }

    private static List<AnimatorResource>? ExtractAll() {
        string? dir = OriginalGameDir();
        if (dir == null) {
            return null;
        }

        var extractor = new AdsExtractor();
        var all = new List<AnimatorResource>();
        foreach (string path in Directory.EnumerateFiles(dir, "*.ADS").OrderBy(p => p)) {
            using FileStream stream = File.OpenRead(path);
            all.Add(extractor.Extract(Path.GetFileName(path), stream));
        }
        return all;
    }

    [Fact]
    public void NoShippedScriptDecompilesToAnUnknownCommand() {
        List<AnimatorResource>? all = ExtractAll();
        if (all == null) {
            return;
        }

        // Guard against a silently empty corpus, which would pass this for the wrong reason.
        Assert.Equal(44, all.Count);
        int animations = all.Sum(a => a.Animations.Count);
        Assert.True(animations > 200, $"corpus looks empty: {animations} animations");

        var unknown = new List<string>();
        foreach (AnimatorResource resource in all) {
            foreach (AnimatorScript script in resource.Animations) {
                foreach (string line in (script.Script ?? string.Empty).Split('\n')) {
                    if (line.TrimStart().StartsWith("UNKNOWN_COMMAND", StringComparison.Ordinal)) {
                        unknown.Add($"{resource.Id} scene {script.Id}: {line.Trim()}");
                    }
                }
            }
        }

        Assert.True(unknown.Count == 0,
            "shipped ADS data now contains opcodes the script language cannot name:\n  "
            + string.Join("\n  ", unknown.Take(20)));
    }

    [Fact]
    public void TheShippedOpcodeSetIsExactlyTheThirteenThatAreNamed() {
        if (OriginalGameDir() == null) {
            return;
        }

        AdsScriptBuilder.SeenCommands.Clear();
        ExtractAll();   // extraction populates SeenCommands as a side effect

        Assert.Equal(Shipped.OrderBy(c => c), AdsScriptBuilder.SeenCommands.OrderBy(c => c));
    }

    [Fact]
    public void TheWeightedRandomBranchFamilyDoesNotShip() {
        if (OriginalGameDir() == null) {
            return;
        }

        AdsScriptBuilder.SeenCommands.Clear();
        ExtractAll();

        // 0x3010 selects one of several weighted alternatives at random; 0x3020 marks the block that
        // follows it. The script language has no construct for either — which costs nothing while
        // neither appears. If one ever does, THAT is when the language needs extending.
        Assert.DoesNotContain((ushort)0x3010, AdsScriptBuilder.SeenCommands);
        Assert.DoesNotContain((ushort)0x3020, AdsScriptBuilder.SeenCommands);
    }
}
