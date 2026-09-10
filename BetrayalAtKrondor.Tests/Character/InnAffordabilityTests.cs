namespace BetrayalAtKrondor.Tests.Character;

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BetrayalAtKrondor.Tests.Content;
using GameData.Resources.Character;
using Xunit;

/// <summary>
/// The nightmaster refuses a party that cannot pay, and the refusal is in the shipped dialog.
/// </summary>
/// <remarks>
/// <b>The port charged anyway and drove party gold negative</b> — 30 royals in, a 5-sovereign
/// (50-royal) room accepted, out at -20. The engine cannot reach that state: the accept branch is
/// gated on Var 3. The rule was documented as "no balance check anywhere on the path", which is
/// true of the C and false of the dialog, and reading only the offer record's own branches finds a
/// Var 0 test and no Var 3 — the gate is one hop further, on the accept path.
/// </remarks>
public class InnAffordabilityTests {
    private const string RefusalKey = "base:ddx:dial_z13:16267";

    [Fact]
    public void ExactlyTheAskingPriceIsEnough() {
        // Var 3 is `PartyGold >= price` and the branch tests Min = 1, so equality stays.
        Assert.True(InnStay.CanAfford(50, 50));
        Assert.True(InnStay.CanAfford(51, 50));
        Assert.False(InnStay.CanAfford(49, 50));
    }

    [Fact]
    public void APartyAlreadyInDebtIsRefused() {
        // Negative gold is reachable only by the bug this guards, but once a save carries it the
        // rule must not sell another night on top.
        Assert.False(InnStay.CanAfford(-20, 50));
    }

    [Fact]
    public void TheRefusalRecordIsTheOneTheShippedDialogNames() {
        Assert.Equal(RefusalKey, InnStay.RefusedDialogKey);
    }

    /// <summary>
    /// Pins the DATA the rule is derived from: the gate exists, tests Var 3, and its fall-through
    /// is the refusal. Skipped when the extracted DDX is absent (no game data on this machine).
    /// </summary>
    [Fact]
    public void TheShippedDialogGatesTheAcceptBranchOnVar3() {
        string? generatedRoot = GeneratedCorpus.FindDir("DDX");
        if (generatedRoot == null) {
            return;   // same skip-if-absent contract the other corpus tests use
        }

        using JsonDocument doc =
            JsonDocument.Parse(File.ReadAllText(Path.Combine(generatedRoot, "DDX", "DIAL_Z13.json")));
        Dictionary<int, JsonElement> byOffset = IndexByOffset(doc.RootElement);

        // The router on the accept path: a text-less entry whose conditional branch is Var 3.
        Assert.True(byOffset.TryGetValue(16238, out JsonElement gate));
        JsonElement branch = gate.GetProperty("Branches")[0];
        Assert.Equal(3, branch.GetProperty("Condition").GetProperty("Var").GetInt32());
        Assert.Equal(1, branch.GetProperty("Condition").GetProperty("Min").GetInt32());

        // …and falling through it lands on the refusal, which is terminal so nothing is charged.
        Assert.Equal(16267, gate.GetProperty("Branches")[1].GetProperty("TargetOffset").GetInt32());
        Assert.True(byOffset.TryGetValue(16267, out JsonElement refused));
        Assert.Contains("haven't any money", refused.GetProperty("Text").GetString());
        Assert.Equal(0, refused.GetProperty("Branches").GetArrayLength());
    }

    private static Dictionary<int, JsonElement> IndexByOffset(JsonElement root) {
        var map = new Dictionary<int, JsonElement>();
        Walk(root, map);

        return map;
    }

    private static void Walk(JsonElement node, Dictionary<int, JsonElement> map) {
        switch (node.ValueKind) {
            case JsonValueKind.Object:
                if (node.TryGetProperty("Offset", out JsonElement off) && off.ValueKind == JsonValueKind.Number) {
                    map[off.GetInt32()] = node;
                }
                foreach (JsonProperty p in node.EnumerateObject()) {
                    Walk(p.Value, map);
                }
                break;
            case JsonValueKind.Array:
                foreach (JsonElement e in node.EnumerateArray()) {
                    Walk(e, map);
                }
                break;
        }
    }
}
