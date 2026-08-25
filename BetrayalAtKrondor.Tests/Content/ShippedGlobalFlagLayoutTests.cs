namespace BetrayalAtKrondor.Tests.Content;

using GameData.Resources.GameState;
using ResourceExtraction;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Xunit;

/// <summary>
/// The high-bitmap packing checked against the ORIGINAL'S OWN SAVE — the verification TASK-209 and
/// TASK-210 both recorded as missing.
/// </summary>
/// <remarks>
/// <b>A round trip between our reader and our writer proves nothing</b> — they share the position
/// arithmetic, which is how TASK-203 and TASK-209 both stayed hidden. This asks a different
/// question: <c>SAVE02.GAM</c> was written by the original game, so the bits it has set must decode
/// to flag ids that shipped content actually uses.
///
/// <para><b>They do, and only under the correct packing.</b> Both high bits set in SAVE02 decode to
/// ids the shipped dialogs write; the linear reading decodes the same two bits to ids <b>no dialog
/// writes at all</b>. That is the discrimination the structural arguments (51 bytes for a 50-byte
/// block; every shipped id on bits 0-7) could only imply.</para>
/// </remarks>
public class ShippedGlobalFlagLayoutTests {
    private const int HeaderSize = 100;

    private static string? GamePath(string relative) {
        var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
        while (dir != null) {
            string candidate = Path.Combine(dir.FullName, "OriginalGame", relative);
            if (File.Exists(candidate)) {
                return candidate;
            }
            dir = dir.Parent;
        }
        return null;   // skip-if-absent, like the other shipped-data tests
    }

    /// <summary>Every flag id the shipped dialogs write, from the committed DDX corpus.</summary>
    private static HashSet<int> DialogFlagIds(string generatedRoot) {
        var ids = new HashSet<int>();
        void Walk(JsonElement e) {
            if (e.ValueKind == JsonValueKind.Object) {
                string type = e.TryGetProperty("$type", out JsonElement t) ? t.GetString() ?? "" : "";
                if (type.EndsWith("SetFlagEffect") && e.TryGetProperty("Flag", out JsonElement f)
                    && f.ValueKind == JsonValueKind.Number) {
                    ids.Add(f.GetInt32());
                }
                if (type.EndsWith("SetFlagsEffect") && e.TryGetProperty("Flags", out JsonElement fl)) {
                    foreach (JsonElement one in fl.EnumerateArray()) {
                        if (one.TryGetProperty("Flag", out JsonElement k)
                            && k.ValueKind == JsonValueKind.Number) {
                            ids.Add(k.GetInt32());
                        }
                    }
                }
                if (type.EndsWith("RawGlobalWriteEffect") && e.TryGetProperty("Key", out JsonElement rk)
                    && rk.ValueKind == JsonValueKind.Number) {
                    ids.Add(rk.GetInt32());
                }
                foreach (JsonProperty p in e.EnumerateObject()) {
                    Walk(p.Value);
                }
            } else if (e.ValueKind == JsonValueKind.Array) {
                foreach (JsonElement one in e.EnumerateArray()) {
                    Walk(one);
                }
            }
        }
        foreach (string file in Directory.GetFiles(Path.Combine(generatedRoot, "DDX"), "DIAL_*.json")) {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(file));
            Walk(doc.RootElement);
        }
        return ids;
    }

    [Fact]
    public void SAVE02sHighFlagBitsDecodeToIdsTheShippedDialogsWrite() {
        string? save = GamePath(Path.Combine("GAMES", "dir.G01", "SAVE02.GAM"));
        string? generated = GeneratedCorpus.FindDir("DDX");
        if (save == null || generated == null) {
            return;
        }

        byte[] bytes = File.ReadAllBytes(save);
        HashSet<int> dialogIds = DialogFlagIds(generated);

        var ours = new List<int>();
        var linear = new List<int>();
        for (var row = 0; row < SaveGameOffsets.GlobalFlags2Size; row++) {
            byte b = bytes[HeaderSize + SaveGameOffsets.GlobalFlags2 + row];
            for (var bit = 0; bit < 8; bit++) {
                if ((b >> bit & 1) == 0) {
                    continue;
                }
                int cx = (row * GlobalFlagLayout.HighFlagsPerByte) + bit + 1;
                ours.Add((cx - GlobalFlagLayout.HighBias) & 0xffff);
                linear.Add(56000 + (row * 8) + bit);
            }
        }

        Assert.NotEmpty(ours);
        // Every bit the ORIGINAL set decodes to an id shipped content writes...
        Assert.All(ours, id => Assert.Contains(id, dialogIds));
        // ...and the linear reading decodes the SAME bits to ids nothing writes.
        Assert.All(linear, id => Assert.DoesNotContain(id, dialogIds));
    }

    [Fact]
    public void ANewGameHasNOFlagsSetAtAll() {
        // The control: STARTUP.GAM is the new-game template, so a non-zero bit here would mean the
        // offsets are pointing at something else entirely.
        string? startup = GamePath("STARTUP.GAM");
        if (startup == null) {
            return;
        }

        byte[] bytes = File.ReadAllBytes(startup);
        for (var i = 0; i < SaveGameOffsets.GlobalFlagsSize; i++) {
            Assert.Equal(0, bytes[HeaderSize + SaveGameOffsets.GlobalFlags + i]);
        }
        for (var i = 0; i < SaveGameOffsets.GlobalFlags2Size; i++) {
            Assert.Equal(0, bytes[HeaderSize + SaveGameOffsets.GlobalFlags2 + i]);
        }
    }
}
