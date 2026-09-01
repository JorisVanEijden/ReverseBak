namespace BetrayalAtKrondor.Tests.Dialog;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using BetrayalAtKrondor.Tests.Content;
using GameData.Resources.Dialog;
using Xunit;

/// <summary>
/// Squire Phillip's conversation, walked over the SHIPPED corpus rather than a fixture.
/// </summary>
/// <remarks>
/// <b>The bug this pins was reported by a player, not by a test.</b> The conversation that begins
/// "Someone was calling." ran to its last spoken line and then stopped on an empty panel, where his
/// list of topics belongs. Every piece was individually covered — <c>NextLine</c>, the id-addressed
/// branch report, the backdrop rule — and none of them walked the actual chain, so nothing failed.
///
/// <para>The chain is nine spoken entries in DIAL_Z30 ending at a <b>text-less router</b> whose
/// only branch is addressed by <b>id</b> (2000001) rather than by offset — the shared ask-about
/// page, which lives in DIAL_Z20. An in-file walk reads that as a dead end, which is exactly what
/// the player saw. See <c>DialogExecutor.WalkFollowingIdTargets</c> for the crossing.</para>
///
/// <para>Skip-if-absent, like the rest of the corpus tests: no <c>generated/</c>, no assertion.</para>
/// </remarks>
public class PhillipConversationReachesHisTopicsTests {
    /// <summary>"Someone was calling." — the entry the player meets on the road.</summary>
    private const int OpeningOffset = 120988;

    /// <summary>The shared ask-about page every NPC's topic list routes into.</summary>
    private const int AskAboutDialogId = 2000001;

    // The corpus is written with a string enum converter (ResourceExtensions.JsonOptions); a bare
    // options instance cannot read "DialogType": "Normal" back.
    private static readonly JsonSerializerOptions Options =
        new() { Converters = { new JsonStringEnumConverter() } };

    private static Dialog? LoadZ30() {
        string? gen = GeneratedCorpus.FindDir("DDX");
        string? path = gen == null ? null : Path.Combine(gen, "DDX", "DIAL_Z30.json");
        return path == null || !File.Exists(path)
            ? null
            : JsonSerializer.Deserialize<Dialog>(File.ReadAllText(path), Options);
    }

    [Fact]
    public void TheConversationEndsAtAnIdAddressedHopIntoTheSharedTopicPage() {
        Dialog? z30 = LoadZ30();
        if (z30 == null) {
            return;   // generated/ not present — skip, don't fail.
        }

        DialogEntry opening = z30.Entries.Single(e => e.Offset == OpeningOffset);
        Assert.StartsWith("\tSomeone was calling.", opening.Text);

        // Walk it the way the conversation loop does: line, then "is there another".
        var spoken = new List<DialogEntry>();
        DialogEntry? current = opening;
        DialogEntry? last = null;
        for (var hop = 0; hop < 40 && current != null; hop++) {
            spoken.Add(current);
            last = current;
            current = DialogBranchWalker.NextLine(z30, current, _ => null);
        }

        // A real conversation, and every line but the last says something.
        Assert.True(spoken.Count > 5, $"the chain should be a conversation, walked {spoken.Count}");
        Assert.All(spoken.Take(spoken.Count - 1),
            e => Assert.False(string.IsNullOrEmpty(e.Text)));

        // *** THE PART THAT WAS BROKEN, AND WHERE THE WALK ACTUALLY ENDS. *** NextLine's text guard
        // is on the entry it is asked ABOUT, not on the one it returns, so the chain's last step
        // hands back a TEXT-LESS ROUTER — the player's "empty panel". Its only branch is addressed
        // by ID rather than by offset, and an in-file walk has nowhere to go from there.
        DialogEntry router = last!;
        Assert.True(string.IsNullOrEmpty(router.Text), "the conversation ends on a silent router");
        Assert.Null(router.Branches.Single().TargetOffset);

        int? crossing = DialogBranchWalker.IdAddressedTargetOf(router, _ => null);
        Assert.Equal(AskAboutDialogId, crossing);
    }

    [Fact]
    public void TheTopicPageIsAChoiceMenuWithTopicsOnIt() {
        string? gen = GeneratedCorpus.FindDir("DDX");
        string? path = gen == null ? null : Path.Combine(gen, "DDX", "DIAL_Z20.json");
        if (path == null || !File.Exists(path)) {
            return;
        }
        Dialog page = JsonSerializer.Deserialize<Dialog>(File.ReadAllText(path), Options)!;

        // The crossing is only worth making if what it lands on is the menu. This also pins the
        // file: the page lives in Z20 while the conversation is in Z30, which is the whole reason
        // an in-file walk could not reach it.
        DialogEntry menu = page.Entries.Single(e => e.Id == AskAboutDialogId);
        Assert.True(menu.Flags.HasFlag(DialogEntryFlags.ChoiceMenu));
        Assert.True(menu.Branches.Count > 1, "a topic list needs topics");
    }
}
