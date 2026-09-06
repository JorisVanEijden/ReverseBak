namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using Xunit;

/// <summary>
/// ActorNumber is a sentinel as often as it is an id.
/// </summary>
/// <remarks>
/// <b>What this is here to stop coming back.</b> 255 was read literally, answered no name and no
/// portrait, and the absence was then written up as faithful in three places — a comment, a doc
/// remark and a test called <c>…GetNoPlate_AndThatIsFAITHFUL</c>. It is not: the original
/// substitutes the running party speaker before anything looks the id up, so the ask-about page
/// always has a portrait and a "&lt;name&gt; asked about:" heading. A wrong belief with a passing
/// test behind it is the expensive kind, so the sentinels are asserted as behaviour here.
/// </remarks>
public class DialogSpeakerSentinelTests {
    private const int ChapterSpeaker = 2; // global 30005; the party latch seeds at +1

    [Fact]
    public void TwoFiveFiveIsTheRunningPartySpeaker_NotNobody() {
        var speakers = new DialogSpeakerSentinel();
        speakers.Begin(ChapterSpeaker);

        // Before any record names anyone, it is the chapter's speaker + 1.
        Assert.Equal(ChapterSpeaker + 1, speakers.Resolve(0xFF, ChapterSpeaker));

        // A record naming a party member LATCHES, and every later 255 follows it. This is the whole
        // mechanism: the ask-about page carries 255 and speaks as whoever asked.
        Assert.Equal(4, speakers.Resolve(4, ChapterSpeaker));
        Assert.Equal(4, speakers.Resolve(0xFF, ChapterSpeaker));
    }

    [Fact]
    public void AnNpcLatchesSeparatelyFromTheParty() {
        var speakers = new DialogSpeakerSentinel();
        speakers.Begin(ChapterSpeaker);

        // 7 and up is an NPC, and it must not disturb the party latch — they are two registers, and
        // collapsing them makes an NPC answer a later 255.
        Assert.Equal(24, speakers.Resolve(24, ChapterSpeaker));
        Assert.Equal(24, speakers.Resolve(0xFD, ChapterSpeaker));
        Assert.Equal(ChapterSpeaker + 1, speakers.Resolve(0xFF, ChapterSpeaker));
    }

    [Fact]
    public void TwoFiveFourResolvesToTheChapterSpeaker_AndThenLatchesLikeALiteralId() {
        var speakers = new DialogSpeakerSentinel();
        speakers.Begin(ChapterSpeaker);
        speakers.Resolve(5, ChapterSpeaker); // party latch is now 5

        // 0xfe is NOT an else-if in the original: it rewrites the id and falls through the rest of
        // the ladder, so it re-seeds the party latch rather than just reporting a value.
        Assert.Equal(ChapterSpeaker + 1, speakers.Resolve(0xFE, ChapterSpeaker));
        Assert.Equal(ChapterSpeaker + 1, speakers.Resolve(0xFF, ChapterSpeaker));
    }

    [Fact]
    public void TheHighByteIsAPortraitFrame_NotPartOfTheId() {
        var speakers = new DialogSpeakerSentinel();
        speakers.Begin(ChapterSpeaker);

        Assert.Equal(3, speakers.Resolve((1 << 8) | 3, ChapterSpeaker));
    }

    [Fact]
    public void ZeroStaysZeroAndDisturbsNeitherLatch() {
        var speakers = new DialogSpeakerSentinel();
        speakers.Begin(ChapterSpeaker);
        speakers.Resolve(6, ChapterSpeaker);
        speakers.Resolve(30, ChapterSpeaker);

        // 5259 of the 8203 shipped entries have no speaker. If those cleared the latches, the line
        // after every narration would lose its speaker.
        Assert.Equal(0, speakers.Resolve(0, ChapterSpeaker));
        Assert.Equal(6, speakers.Resolve(0xFF, ChapterSpeaker));
        Assert.Equal(30, speakers.Resolve(0xFD, ChapterSpeaker));
    }

    [Fact]
    public void CombatSlotsNameWhoeverTheTextVariableSlotHolds() {
        var speakers = new DialogSpeakerSentinel();
        speakers.Begin(ChapterSpeaker);

        // 0xf0+n is "whoever slot n holds" — the same six-slot table the @N text variables use.
        var table = new DialogSlotTable();
        table.Kinds[0] = 5;
        table.Kinds[1] = 0;

        Assert.Equal(6, speakers.Resolve(0xF0, ChapterSpeaker, table.Kinds)); // actor 5 -> id 6
        Assert.Equal(5, speakers.NewPrimaryActor);

        // It LATCHES, exactly like a literal 1..6: a later 255 follows the combatant just named.
        Assert.Equal(6, speakers.Resolve(0xFF, ChapterSpeaker, table.Kinds));

        Assert.Equal(1, speakers.Resolve(0xF1, ChapterSpeaker, table.Kinds)); // actor 0 -> id 1
        Assert.Equal(0, speakers.NewPrimaryActor);

        // The three named sentinels sit above the range and are NOT table indices.
        Assert.True(DialogSpeakerSentinel.IsCombatSpeakerSlot(0xF0));
        Assert.True(DialogSpeakerSentinel.IsCombatSpeakerSlot(0xF5));
        Assert.False(DialogSpeakerSentinel.IsCombatSpeakerSlot(0xFD));
        Assert.False(DialogSpeakerSentinel.IsCombatSpeakerSlot(0xFE));
        Assert.False(DialogSpeakerSentinel.IsCombatSpeakerSlot(0xFF));
    }

    [Fact]
    public void AnEmptyOrCreatureSlotNamesNobody_AndDisturbsNothing() {
        var speakers = new DialogSpeakerSentinel();
        speakers.Begin(ChapterSpeaker);
        speakers.Resolve(4, ChapterSpeaker); // party latch = 4

        var table = new DialogSlotTable();               // every slot starts NoActor
        table.Kinds[2] = DialogSlotTable.CreatureActor;

        // Both must answer "nobody" rather than fall back to someone: DialogSlotTable makes the
        // same refusal in ResolveActorOperand, and for the same reason.
        Assert.Equal(0, speakers.Resolve(0xF0, ChapterSpeaker, table.Kinds));
        Assert.Null(speakers.NewPrimaryActor);
        Assert.Equal(0, speakers.Resolve(0xF2, ChapterSpeaker, table.Kinds));
        Assert.Null(speakers.NewPrimaryActor);

        // And neither may poison the latch — a caption that goes missing is recoverable, a caption
        // naming the wrong character is not.
        Assert.Equal(4, speakers.Resolve(0xFF, ChapterSpeaker, table.Kinds));
    }

    [Fact]
    public void WithoutASlotTableACombatSlotAnswersNothing() {
        var speakers = new DialogSpeakerSentinel();
        speakers.Begin(ChapterSpeaker);

        // The table is per-play state. A record resolved without one is being asked a question that
        // has no answer yet, so it must not invent one.
        Assert.Equal(0, speakers.Resolve(0xF4, ChapterSpeaker));
        Assert.Null(speakers.NewPrimaryActor);
    }

    [Fact]
    public void TheResolvedSpeakerIsWhatTheNamePlateAndCameraSeeItAs() {
        var speakers = new DialogSpeakerSentinel();
        speakers.Begin(ChapterSpeaker);
        speakers.Resolve(3, ChapterSpeaker);
        int resolved = speakers.Resolve(0xFF, ChapterSpeaker);

        // The two consumers that were reading 255 raw. Both answer for a party id and refuse 255,
        // which is exactly why the page had neither plate nor camera turn.
        Assert.True(DialogSpeakerNamePill.ShowsFor(resolved, DialogEntryFlags.PreserveKeyword));
        Assert.False(DialogSpeakerNamePill.ShowsFor(0xFF, DialogEntryFlags.PreserveKeyword));

        var party = new byte[] { 4, 2, 5 }; // character ids; resolved-1 must be found among them
        Assert.Equal(1, DialogBackdropCamera.SpeakerSlot(party, resolved));
        Assert.Equal(-1, DialogBackdropCamera.SpeakerSlot(party, 0xFF));
    }
}
