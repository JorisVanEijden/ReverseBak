namespace BetrayalAtKrondor.Tests.Content;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

/// <summary>
/// Whether the SHIPPED cutscene scripts satisfy the frame loop's assumptions.
/// </summary>
/// <remarks>
/// <b>Every other cutscene test drives the loop with synthetic frames.</b> They pin what the loop
/// does with a jump, a tag, a missing target — the ORDER logic — and they are the right shape for
/// that. What none of them asks is whether the 42 scripts the game actually ships obey those
/// assumptions, and the loop's own remarks name the two failure modes it cannot see:
/// <i>"a cutscene that loops for ever or one that stops early"</i>, neither of which raises an
/// exception.
///
/// <para>This is the cheap half of the scene-level coverage TASK-159 named as its honest next
/// question. It is not a playthrough — no renderer, no resources, no Unity — but it is measured over
/// the real corpus rather than a fixture, so it fails when the DATA breaks the loop's contract
/// rather than when a hand-written frame does.</para>
///
/// <para><b>Skips rather than fails when <c>generated/</c> is absent</b>, the same contract the other
/// corpus tests use.</para>
/// </remarks>
public class ShippedCutsceneScriptTests {
    private const string TagFrame = "TagFrame";
    private const string GotoFrame = "GotoFrame";

    /// <summary>
    /// A command flattened out of the JSON.
    /// </summary>
    /// <remarks>
    /// <b>Read eagerly on purpose.</b> A <c>JsonElement</c> is a window onto its
    /// <c>JsonDocument</c>, so holding one past the document's <c>using</c> throws
    /// <c>ObjectDisposedException</c> — which is exactly how the first version of this file failed.
    /// Copying the four fields out is both correct and simpler to read than keeping documents alive.
    /// </remarks>
    private sealed record Command(string Type, int? NextFrame, string? TargetKey, int? TagNumber);

    private sealed record Frame(int Index, int? Tag, string Key, IReadOnlyList<Command> Commands);

    private sealed record Script(string Name, IReadOnlyList<Frame> Frames,
        IReadOnlyDictionary<string, string> Tags);

    private static IReadOnlyList<Script> Load() {
        string? dir = GeneratedCorpus.FindDir("TTM");
        if (dir == null) {
            return System.Array.Empty<Script>();
        }

        var scripts = new List<Script>();
        foreach (string path in Directory.EnumerateFiles(Path.Combine(dir, "TTM"), "*.json")
                     .OrderBy(p => p)) {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            JsonElement root = doc.RootElement;
            var frames = new List<Frame>();
            JsonElement frameArray = root.GetProperty("Frames");
            for (var i = 0; i < frameArray.GetArrayLength(); i++) {
                JsonElement f = frameArray[i];
                int? tag = f.TryGetProperty("Tag", out JsonElement t)
                    && t.ValueKind == JsonValueKind.Number ? t.GetInt32() : null;
                string key = f.TryGetProperty("Key", out JsonElement k) ? k.GetString() ?? "" : "";
                var commands = new List<Command>();
                foreach (JsonElement c in f.GetProperty("Commands").EnumerateArray()) {
                    commands.Add(new Command(
                        c.TryGetProperty("$type", out JsonElement ty) ? ty.GetString() ?? "" : "",
                        c.TryGetProperty("NextFrame", out JsonElement n) ? n.GetInt32() : null,
                        c.TryGetProperty("TargetKey", out JsonElement k2) ? k2.GetString() : null,
                        c.TryGetProperty("TagNumber", out JsonElement tn) ? tn.GetInt32() : null));
                }
                frames.Add(new Frame(i, tag, key, commands));
            }

            var tags = new Dictionary<string, string>();
            if (root.TryGetProperty("Tags", out JsonElement tagMap)
                && tagMap.ValueKind == JsonValueKind.Object) {
                foreach (JsonProperty p in tagMap.EnumerateObject()) {
                    tags[p.Name] = p.Value.GetString() ?? "";
                }
            }

            scripts.Add(new Script(Path.GetFileName(path), frames, tags));
        }
        return scripts;
    }

    [Fact]
    public void EveryJumpLandsOnAFrameThatCarriesItsTag() {
        // *** THE FAILURE THIS CATCHES IS SILENT. *** The loop's own remark records "a missing tag
        // that was silently ignored" as a bug it has had. A jump whose tag names no frame does not
        // throw; the scene simply runs on, or stops. Measured over the shipped corpus, not a
        // fixture, so it is the DATA being checked against the loop's contract.
        IReadOnlyList<Script> scripts = Load();
        if (scripts.Count == 0) {
            return;   // generated/ not present — skip, do not fail
        }

        var unresolved = new List<string>();
        var jumps = 0;
        foreach (Script s in scripts) {
            foreach (Frame f in s.Frames) {
                foreach (Command c in f.Commands) {
                    if (c.Type != GotoFrame || c.NextFrame == null) {
                        continue;
                    }
                    jumps++;
                    int target = c.NextFrame.Value;
                    if (!s.Frames.Any(other => other.Tag == target)) {
                        unresolved.Add($"{s.Name} frame {f.Index} -> tag {target}");
                    }
                }
            }
        }

        Assert.True(jumps > 0, "no GotoFrame in the corpus — the check would be vacuous");
        Assert.Empty(unresolved);
    }

    [Fact]
    public void AJumpsTargetKeyMatchesTheKeyOfTheFrameItLandsOn() {
        // The de-indexed TargetKey and the numeric NextFrame are two spellings of one destination.
        // If they ever disagree, whichever the consumer reads decides the scene — and a content
        // pipeline that regenerated one without the other would produce exactly that.
        IReadOnlyList<Script> scripts = Load();
        if (scripts.Count == 0) {
            return;
        }

        var mismatched = new List<string>();
        foreach (Script s in scripts) {
            foreach (Frame f in s.Frames) {
                foreach (Command c in f.Commands) {
                    if (c.Type != GotoFrame || c.TargetKey == null || c.NextFrame == null) {
                        continue;
                    }
                    Frame? landing = s.Frames.FirstOrDefault(other => other.Tag == c.NextFrame);
                    if (landing != null && landing.Key != c.TargetKey) {
                        mismatched.Add(
                            $"{s.Name} frame {f.Index}: '{c.TargetKey}' vs '{landing.Key}'");
                    }
                }
            }
        }

        Assert.Empty(mismatched);
    }

    [Fact]
    public void AFrameTaggedInItsCommandsCarriesThatTagOnTheFrame() {
        // The tag reaches the frame two ways: a TagFrame COMMAND inside it, and a Tag FIELD on it.
        // The loop reads the field. If the extractor ever stopped lifting the command onto the
        // frame, every jump would land nowhere and nothing else here would notice.
        IReadOnlyList<Script> scripts = Load();
        if (scripts.Count == 0) {
            return;
        }

        var mismatched = new List<string>();
        var tagged = 0;
        foreach (Script s in scripts) {
            foreach (Frame f in s.Frames) {
                foreach (Command c in f.Commands) {
                    if (c.Type != TagFrame || c.TagNumber == null) {
                        continue;
                    }
                    tagged++;
                    if (f.Tag != c.TagNumber) {
                        mismatched.Add(
                            $"{s.Name} frame {f.Index}: command {c.TagNumber}, field {f.Tag}");
                    }
                }
            }
        }

        Assert.True(tagged > 0, "no TagFrame in the corpus — the check would be vacuous");
        Assert.Empty(mismatched);
    }

    [Fact]
    public void NoShippedScriptIsEmpty() {
        // A script with no frames plays nothing and reports nothing. Cheap to assert, and it is the
        // shape an extraction regression takes when a format detail moves.
        IReadOnlyList<Script> scripts = Load();
        if (scripts.Count == 0) {
            return;
        }

        Assert.Empty(scripts.Where(s => s.Frames.Count == 0).Select(s => s.Name));
    }
}
