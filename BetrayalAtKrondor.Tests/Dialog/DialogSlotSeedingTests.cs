namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// The standing slot defaults every dialog play starts from —
/// <c>dialog_combatant_name_table_init</c> (DIALOG.C:777-793), called once by
/// <c>dialog_play_record</c> (DIALOG.C:849) before it walks any record.
///
/// <para>It clears all six slots, then seeds four of them <b>in this order</b>: slot 4 (kind 7),
/// slot 5 (kind 15), slot 3 (kind 14), slot 0 (kind 31). The order matters because the random
/// picker only refuses actors already used in a LOWER slot.</para>
/// </summary>
[Collection(BetrayalAtKrondor.Tests.Text.UiStringsCollection.Name)]
public class DialogSlotSeedingTests {
    // Member ids as the engine numbers them; the picker's constraints are in these.
    private const int Locklear = 0, Gorath = 1, Owyn = 2, Pug = 3, James = 4, Patrus = 5;

    private static readonly string[] Names =
        { "Locklear", "Gorath", "Owyn", "Pug", "James", "Patrus" };

    /// <summary>An RND that walks the roster in order and then repeats, so which candidate is
    /// offered when is fully determined — the constraints, not the draw, decide the outcome.</summary>
    private static DialogSlotContext Context(IReadOnlyList<int> roster, int chapterSpeaker) {
        int n = 0;
        return new DialogSlotContext {
            PartyRoster = roster,
            ActorNames = Names,
            ChapterSpeakerId = chapterSpeaker,
            CurrentActorId = chapterSpeaker,
            Random = bound => bound <= 0 ? 0 : n++ % bound,
        };
    }

    [Fact]
    public void Slot4IsTheChapterSpeaker() {
        // Kind 7 reads global 30005 directly — no randomness. This is the @4 of every narration
        // line ("@4 checked their funds"), and the slot that used to come out empty.
        var ctx = Context(new[] { Locklear, Gorath, Owyn }, chapterSpeaker: Locklear);
        DialogSlotTable table = DialogSlotPopulator.CreateForPlay(ctx);
        Assert.Equal("Locklear", table.Names[4]);
        Assert.Equal(Locklear, table.Kinds[4]);
    }

    [Fact]
    public void Slot4FollowsTheChapterSpeakerRatherThanTheRosterOrder() {
        var ctx = Context(new[] { Locklear, Gorath, Owyn }, chapterSpeaker: Owyn);
        DialogSlotTable table = DialogSlotPopulator.CreateForPlay(ctx);
        Assert.Equal("Owyn", table.Names[4]);
    }

    [Fact]
    public void Slot5TakesAnActorFromItsOwnSetAndNotSlot4s() {
        // Kind 15 accepts only members {0,1,4}; slot 4 already holds Locklear(0), and the
        // dedupe-against-lower-slots rule excludes him, leaving Gorath(1).
        var ctx = Context(new[] { Locklear, Gorath, Owyn }, chapterSpeaker: Locklear);
        DialogSlotTable table = DialogSlotPopulator.CreateForPlay(ctx);
        Assert.Equal("Gorath", table.Names[5]);
        Assert.Equal(Gorath, table.Kinds[5]);
    }

    [Fact]
    public void Slot3TakesAnActorFromItsOwnSet() {
        // Kind 14 accepts only members {2,3,5}. Owyn(2) is the only one present.
        var ctx = Context(new[] { Locklear, Gorath, Owyn }, chapterSpeaker: Locklear);
        DialogSlotTable table = DialogSlotPopulator.CreateForPlay(ctx);
        Assert.Equal("Owyn", table.Names[3]);
    }

    [Fact]
    public void Slot3IgnoresSlots4And5WhenDeduplicating() {
        // Slot 3 is seeded THIRD but sits below 4 and 5, and the picker only looks at lower slots —
        // so it may legitimately repeat an actor that slot 4 or 5 already took. Party = {Owyn} only
        // for kind 14's set, and Owyn is also reachable by slot 4 here.
        var ctx = Context(new[] { Owyn, Pug }, chapterSpeaker: Owyn);
        DialogSlotTable table = DialogSlotPopulator.CreateForPlay(ctx);
        Assert.Equal("Owyn", table.Names[4]);
        Assert.Contains(table.Names[3], new[] { "Owyn", "Pug" });
    }

    [Fact]
    public void Slot0IsNeverTheChapterSpeaker() {
        // Kind 31 rejects exactly the member global 30005 names, so @0 and @4 never collide.
        var ctx = Context(new[] { Locklear, Gorath, Owyn }, chapterSpeaker: Locklear);
        DialogSlotTable table = DialogSlotPopulator.CreateForPlay(ctx);
        Assert.NotEqual("Locklear", table.Names[0]);
        Assert.NotEqual(Locklear, table.Kinds[0]);
    }

    [Fact]
    public void UnsatisfiableConstraintFallsBackToTheFirstRosterMember() {
        // Kind 14 wants {2,3,5} and this party has none of them. The engine gives up after 500
        // draws and takes party_roster[0] rather than looping forever.
        var ctx = Context(new[] { Locklear, Gorath }, chapterSpeaker: Locklear);
        DialogSlotTable table = DialogSlotPopulator.CreateForPlay(ctx);
        Assert.Equal("Locklear", table.Names[3]);
    }

    [Fact]
    public void EmptyPartyLeavesTheRandomSlotsEmptyWithoutHanging() {
        // party_count == 0 makes the picker return immediately, having assigned nothing.
        var ctx = Context(new int[0], chapterSpeaker: Locklear);
        DialogSlotTable table = DialogSlotPopulator.CreateForPlay(ctx);
        Assert.Equal("", table.Names[0]);
        Assert.Equal("", table.Names[3]);
        Assert.Equal("", table.Names[5]);
    }

    [Fact]
    public void SlotsOneAndTwoAreNotSeeded() {
        // Only 4, 5, 3 and 0 have standing defaults. 1 and 2 stay empty until an entry fills them.
        var ctx = Context(new[] { Locklear, Gorath, Owyn }, chapterSpeaker: Locklear);
        DialogSlotTable table = DialogSlotPopulator.CreateForPlay(ctx);
        Assert.Equal("", table.Names[1]);
        Assert.Equal("", table.Names[2]);
    }

    [Fact]
    public void EveryPlayReSeeds() {
        // The picker re-rolls per play, so two plays of the same dialog may name different
        // companions. What must hold is that each play is internally consistent.
        var ctx = Context(new[] { Locklear, Gorath, Owyn, Pug, James, Patrus }, chapterSpeaker: James);
        DialogSlotTable first = DialogSlotPopulator.CreateForPlay(ctx);
        DialogSlotTable second = DialogSlotPopulator.CreateForPlay(ctx);
        Assert.Equal("James", first.Names[4]);
        Assert.Equal("James", second.Names[4]);
        Assert.NotEqual("James", first.Names[0]);
        Assert.NotEqual("James", second.Names[0]);
    }
}
