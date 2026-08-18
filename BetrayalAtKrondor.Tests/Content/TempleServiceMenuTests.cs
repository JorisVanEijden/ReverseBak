namespace BetrayalAtKrondor.Tests.Content;

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using GameData.Resources.Scene;
using Xunit;

/// <summary>
/// The temple service menu's results are branch POSITIONS in the shipped dialog, so the constants
/// only mean what they say while that dialog's options stay in their order. Skip-if-absent.
/// </summary>
public class TempleServiceMenuTests {
    /// <summary>The dialog every action-13 hotspot shows — all 15 of them use this one.</summary>
    private const int ServiceDialogId = 1300072;

    /// <summary>Keyword ids, as they appear on the branches (1-based into the catalog).</summary>
    private const int Talk = 269, Cure = 272, Bless = 271, Done = 268;

    [Fact]
    public void TheServiceDialogsOptionsAreStillInTheOrderTheConstantsAssume() {
        string? gen = GeneratedCorpus.FindDir(Path.Combine("DAT", "keywords.json"), "DDX");
        if (gen == null) {
            return;
        }

        List<int> flags = ChoiceFlagsOf(gen, ServiceDialogId);

        // Talk, Cure, Bless, Done — one option per branch, in this order. The heal is the option at
        // index 1 and the way out is the one at index 3, which is the whole of why the dispatch
        // constants read 1, 2 and 3.
        Assert.Equal(new[] { Talk, Cure, Bless, Done }, flags);
        Assert.Equal(Cure, flags[GdsActionDispatch.HealingService]);
        Assert.Equal(Bless, flags[GdsActionDispatch.BlessingService]);
        Assert.Equal(Done, flags[GdsActionDispatch.ServiceMenuExitResult]);
    }

    [Fact]
    public void EveryTempleServiceHotspotShowsThatSameDialog() {
        string? gen = GeneratedCorpus.FindDir(Path.Combine("DAT", "keywords.json"), "GDS");
        if (gen == null) {
            return;
        }

        var found = 0;
        foreach (string path in Directory.GetFiles(Path.Combine(gen, "GDS"), "*.json")) {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("Hotspots", out JsonElement hotspots)) {
                continue;
            }
            foreach (JsonElement hotspot in hotspots.EnumerateArray()) {
                if (hotspot.GetProperty("ActionCode").GetInt32() != ServiceActionCode) {
                    continue;
                }
                found++;
                // One dialog for every temple, which is what lets one set of constants describe
                // them all — a shrine with its own service dialog could order its options
                // differently and mean something else by 1.
                Assert.Equal(ServiceDialogId, hotspot.GetProperty("ActionDialogId").GetInt32());
            }
        }

        Assert.True(found > 0, "no service hotspot found in the shipped scenes");
    }

    private const int ServiceActionCode = 13;

    /// <summary>The flags of a dialog entry's choice branches, in the order they are offered.</summary>
    private static List<int> ChoiceFlagsOf(string generatedRoot, int dialogId) {
        var flags = new List<int>();
        foreach (string path in Directory.GetFiles(Path.Combine(generatedRoot, "DDX"), "*.json")) {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            if (TryCollect(doc.RootElement, dialogId, flags)) {
                break;
            }
        }

        return flags;
    }

    private static bool TryCollect(JsonElement element, int dialogId, List<int> flags) {
        if (element.ValueKind == JsonValueKind.Object) {
            if (element.TryGetProperty("Id", out JsonElement id)
                && id.ValueKind == JsonValueKind.Number && id.GetInt32() == dialogId) {
                foreach (JsonElement branch in element.GetProperty("Branches").EnumerateArray()) {
                    flags.Add(branch.GetProperty("Condition").GetProperty("Flag").GetInt32());
                }

                return true;
            }
            foreach (JsonProperty property in element.EnumerateObject()) {
                if (TryCollect(property.Value, dialogId, flags)) {
                    return true;
                }
            }
        } else if (element.ValueKind == JsonValueKind.Array) {
            foreach (JsonElement item in element.EnumerateArray()) {
                if (TryCollect(item, dialogId, flags)) {
                    return true;
                }
            }
        }

        return false;
    }
}
