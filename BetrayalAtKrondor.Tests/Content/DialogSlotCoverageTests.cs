namespace BetrayalAtKrondor.Tests.Content;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using GameData.Resources.Dialog;
using Xunit;

/// <summary>
/// Corpus-wide guard that no <c>@N</c> placeholder can render as nothing: for every entry in every
/// shipped DIAL_Z*.DDX, each <c>@N</c> its text contains must land on a slot that <b>every</b> route
/// reaching that entry has written.
///
/// <para>Why routes matter: <c>dialog_play_record</c> seeds the six slots once per play
/// (DIALOG.C:849) and then applies each walked record's ops on the way down (DIALOG.C:861), so a
/// leaf's <c>@1</c> is legitimately supplied by a text-less router above it — 26 entries in the
/// shipped corpus are filled that way and by nothing else. This walks the branch graph to a
/// fixpoint and takes the <i>intersection</i> over incoming routes, so a token filled on only some
/// paths still fails.</para>
///
/// <para>Which slots a set of ops writes is asked of <see cref="DialogSlotPopulator"/> itself rather
/// than restated here — that keeps the guard honest about the rules that are easy to get wrong
/// (which four slots the seeding fills, and kind 27 writing slots 0 and 1 whatever slot it was
/// asked for). A slot the populator leaves empty is a slot the renderer emits nothing for, which is
/// the failure this test exists to catch.</para>
///
/// <para>History: TASK-12 → TASK-71 (slot seeding) → TASK-72 (kinds, intermediate-entry ops). Each
/// closed on live spot-checks of individual dialogs; this is the corpus-wide statement none of them
/// made. Skip-if-absent, like the other <c>generated/</c> reference tests.</para>
/// </summary>
public class DialogSlotCoverageTests {
    private static readonly Regex Token = new(@"@(\d)", RegexOptions.Compiled);

    [Fact]
    public void EveryDialogTokenLandsOnASlotEveryRouteFills() {
        string? gen = GeneratedCorpus.FindDir("DDX");
        if (gen == null) {
            return; // generated/ not present (e.g. CI without game data) — skip, don't fail.
        }

        var unresolved = new List<string>();
        int checkedTokens = 0;

        foreach (string path in Directory.GetFiles(Path.Combine(gen, "DDX"), "DIAL_Z*.json")) {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            Entry[] entries = doc.RootElement.GetProperty("Entries").EnumerateArray()
                .Select(Entry.From).ToArray();
            string file = Path.GetFileNameWithoutExtension(path);

            IReadOnlyDictionary<string, ISet<int>> filled = FilledSlotsPerEntry(entries);

            foreach (Entry e in entries) {
                foreach (Match m in Token.Matches(e.Text)) {
                    checkedTokens++;
                    int slot = int.Parse(m.Groups[1].Value);
                    if (!filled[e.Key].Contains(slot)) {
                        unresolved.Add($"{file} {e.Key} @{slot} | {Condense(e.Text)}");
                    }
                }
            }
        }

        Assert.True(checkedTokens > 0, "no @N tokens found — the corpus or the reader is wrong");
        Assert.True(unresolved.Count == 0,
            $"{unresolved.Count} dialog token(s) would render as nothing:\n  "
            + string.Join("\n  ", unresolved.Take(25)));
    }

    /// <summary>
    /// Slots guaranteed non-empty at each entry: its own ops, plus what every route into it has
    /// already written (the seeding, for an entry nothing branches to). Iterated to a fixpoint
    /// because the branch graph has cycles.
    /// </summary>
    private static IReadOnlyDictionary<string, ISet<int>> FilledSlotsPerEntry(Entry[] entries) {
        var parents = new Dictionary<string, List<string>>();
        foreach (Entry e in entries) {
            foreach (string target in e.Branches) {
                if (!parents.TryGetValue(target, out List<string>? list)) {
                    parents[target] = list = new List<string>();
                }
                list.Add(e.Key);
            }
        }

        ISet<int> seeded = SlotsWritten(Array.Empty<Op>());
        var own = entries.ToDictionary(e => e.Key, e => SlotsWritten(e.Ops));

        // Start optimistic (every slot) so the intersection can only shrink toward the fixpoint.
        var all = new HashSet<int>(Enumerable.Range(0, DialogSlotTable.SlotCount));
        var filled = entries.ToDictionary(e => e.Key, _ => (ISet<int>)new HashSet<int>(all));

        for (bool changed = true; changed;) {
            changed = false;
            foreach (Entry e in entries) {
                ISet<int> inherited = parents.TryGetValue(e.Key, out List<string>? ps)
                    ? ps.Select(p => filled[p]).Aggregate(new HashSet<int>(all),
                        (acc, s) => { acc.IntersectWith(s); return acc; })
                    : new HashSet<int>(seeded);
                inherited.UnionWith(own[e.Key]);
                if (!inherited.SetEquals(filled[e.Key])) {
                    filled[e.Key] = inherited;
                    changed = true;
                }
            }
        }
        return filled;
    }

    /// <summary>Ask the real populator which slots a play's seeding plus <paramref name="ops"/>
    /// leave non-empty. A slot is "written" when it holds text, which is exactly what the renderer
    /// needs and what a token that resolves to nothing is missing.</summary>
    private static ISet<int> SlotsWritten(IReadOnlyList<Op> ops) {
        DialogSlotContext context = Context();
        DialogSlotTable table = DialogSlotPopulator.CreateForPlay(context);
        if (ops.Count > 0) {
            table.Clear(); // ops alone, so the caller can union the seeding in separately
            foreach (Op op in ops) {
                DialogSlotPopulator.Assign(table, op.Slot, op.Source, op.Aux, context);
            }
        }
        var written = new HashSet<int>();
        for (int i = 0; i < DialogSlotTable.SlotCount; i++) {
            if (!string.IsNullOrEmpty(table.Names[i])) {
                written.Add(i);
            }
        }
        return written;
    }

    /// <summary>A deliberately fully-populated context: a full six-member party, every lookup
    /// answering, every global non-zero. The question this test asks is "does anything write this
    /// slot", not "is the value right", so nothing may come back empty for want of state.</summary>
    private static DialogSlotContext Context() => new() {
        PartyRoster = new[] { 0, 1, 2, 3, 4, 5 },
        ActorNames = new[] { "Locklear", "Gorath", "Owyn", "Pug", "James", "Patrus" },
        ChapterSpeakerId = 0,
        CurrentActorId = 1,
        PrimaryActorId = 1,
        SecondaryActorId = 2,
        TertiaryActorId = 3,
        CreatureType = 20,
        KeyObjectId = 48,
        PartyMoneyInRoyals = 1234,
        QuotedAmount = 500,
        Global30015 = 3,
        Global30018 = 4,
        CreatureNameOf = _ => "goblin",
        ObjectNameOf = _ => "sword",
        AttributeValueOf = _ => 42,
        Random = bound => bound <= 0 ? 0 : 0,
    };

    private static string Condense(string text) {
        string one = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return one.Length <= 90 ? one : one[..90];
    }

    private readonly record struct Op(int Slot, int Source, int Aux);

    private readonly record struct Entry(string Key, string Text, Op[] Ops, string[] Branches) {
        public static Entry From(JsonElement e) => new(
            e.GetProperty("Key").GetString()!,
            e.TryGetProperty("Text", out JsonElement t) && t.ValueKind == JsonValueKind.String
                ? t.GetString()! : "",
            e.TryGetProperty("Actions", out JsonElement a) && a.ValueKind == JsonValueKind.Array
                ? a.EnumerateArray()
                    .Where(x => x.TryGetProperty("$type", out JsonElement ty)
                                && ty.GetString() == "SetTextVariable")
                    .Select(x => new Op(x.GetProperty("Slot").GetInt32(),
                        x.GetProperty("Source").GetInt32(), x.GetProperty("Aux").GetInt32()))
                    .ToArray()
                : Array.Empty<Op>(),
            e.TryGetProperty("Branches", out JsonElement b) && b.ValueKind == JsonValueKind.Array
                ? b.EnumerateArray()
                    .Where(x => x.TryGetProperty("TargetKey", out JsonElement k)
                                && k.ValueKind == JsonValueKind.String)
                    .Select(x => x.GetProperty("TargetKey").GetString()!)
                    .ToArray()
                : Array.Empty<string>());
    }
}
