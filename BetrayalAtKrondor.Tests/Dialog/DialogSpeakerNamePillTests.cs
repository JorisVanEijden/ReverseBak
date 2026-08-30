namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using Xunit;

/// <summary>
/// The rounded name plate under the speaker's portrait.
/// </summary>
/// <remarks>
/// The traps these pin down: the plate is gated on a flag that reads like a text-formatting flag,
/// its rounded ends sit OUTSIDE the bar they cap, and the two halves of the name lookup (party
/// versus keyword table) are easy to collapse into one.
/// </remarks>
public class DialogSpeakerNamePillTests {
    [Fact]
    public void AFlagThatReadsLikeFormattingIsWhatDecidesIt() {
        // The original looks the name up either way and then discards it when the flag is clear, so
        // a port that only checks the speaker id captions every narrator in the game.
        Assert.True(DialogSpeakerNamePill.ShowsFor(4, DialogEntryFlags.PreserveKeyword));
        Assert.False(DialogSpeakerNamePill.ShowsFor(4, DialogEntryFlags.IsolatePalette));
    }

    [Fact]
    public void ASpeakerlessEntryIsNotCaptioned() {
        Assert.False(DialogSpeakerNamePill.ShowsFor(0, DialogEntryFlags.PreserveKeyword));
    }

    [Theory]
    [InlineData(0x45, true)]
    [InlineData(0x46, false)]
    [InlineData(0x47, false)]
    public void TheIdCutoffIsExclusive(int speakerId, bool captioned) {
        Assert.Equal(captioned,
            DialogSpeakerNamePill.ShowsFor(speakerId, DialogEntryFlags.PreserveKeyword));
    }

    [Fact]
    public void TheEntryOverloadReadsTheLowByteOfTheActorNumber() {
        // The high byte is a portrait variant, not part of the id — including it would push every
        // speaker past the cutoff and silently drop the plate.
        var entry = new DialogEntry {
            ActorNumber = 0x0304,
            Flags = DialogEntryFlags.PreserveKeyword,
        };
        Assert.True(DialogSpeakerNamePill.ShowsFor(entry));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(6, true)]
    [InlineData(7, false)]
    public void ThePartyAndTheKeywordTableSplitAtSeven(int speakerId, bool party) {
        Assert.Equal(party, DialogSpeakerNamePill.IsPartySpeaker(speakerId));
    }

    [Fact]
    public void PartyIdsAreOneBasedAndKeywordIdsAreOffset() {
        Assert.Equal(0, DialogSpeakerNamePill.PartyIndexOf(1));
        Assert.Equal(7 + 0x124, DialogSpeakerNamePill.KeywordIndexOf(7));
    }

    [Fact]
    public void AShortNameStillGetsTheMinimumBar() {
        Assert.Equal(DialogSpeakerNamePill.MinWidth, DialogSpeakerNamePill.BarWidth(0));
    }

    [Fact]
    public void ALongNameGrowsTheBarByItsPadding() {
        const int wide = DialogSpeakerNamePill.MinWidth * 2;
        Assert.Equal(wide + DialogSpeakerNamePill.LabelPadding,
            DialogSpeakerNamePill.BarWidth(wide));
    }

    [Fact]
    public void TheCapsAddADiameterBecauseTheySitOnTheBarsEnds() {
        // *** The failure this catches. *** The caps are circles centred ON the bar's ends, not
        // tucked inside it, so sizing a rounded rectangle to the bar width alone draws a plate two
        // radii too narrow — which reads as "the name barely fits" rather than as a wrong number.
        Assert.Equal(DialogSpeakerNamePill.BarWidth(0) + (2 * DialogSpeakerNamePill.CapRadius),
            DialogSpeakerNamePill.OuterWidth(0));
    }

    [Fact]
    public void ThePlateIsCentredOnTheScreenWhateverTheNameIs() {
        foreach (float labelWidth in new[] { 0f, 200f, 600f }) {
            int left = DialogSpeakerNamePill.Left(labelWidth);
            int centre = left + (DialogSpeakerNamePill.OuterWidth(labelWidth) / 2);
            Assert.Equal(DialogSpeakerNamePill.CentreX, centre);
        }
    }

    [Fact]
    public void TheLabelIsCentredTwoOriginalPixelsRightOfThePlate() {
        // In the original, not a rounding artefact of this port: 0xA0 against 0x9E.
        Assert.Equal(2 * 5, DialogSpeakerNamePill.LabelCentreX - DialogSpeakerNamePill.CentreX);
    }

    [Fact]
    public void TheLabelSitsInsideThePlateVertically() {
        Assert.InRange(DialogSpeakerNamePill.LabelTop,
            DialogSpeakerNamePill.Top, DialogSpeakerNamePill.Bottom);
    }
    [Fact]
    public void TheKeywordHalfRunsFromSevenToFiftyThree_AndTheNameBlockIsExactlyThatLong() {
        // Established from the shipped KEYWORD.DAT: 346 entries, lookup reads word id+0x124, so the
        // last reachable id is 345-292 = 53. Entries 295-298 are EMPTY and the names start at 299 =
        // 7 + 292 — the id range and the table's name block are the same 47 slots, which is what
        // makes this a bound rather than a coincidence.
        Assert.Equal(7, DialogSpeakerNamePill.FirstKeywordSpeakerId);
        Assert.Equal(299, DialogSpeakerNamePill.KeywordIndexOf(
            DialogSpeakerNamePill.FirstKeywordSpeakerId));
        Assert.Equal(345, DialogSpeakerNamePill.KeywordIndexOf(
            DialogSpeakerNamePill.LastKeywordSpeakerId));
    }

    [Fact]
    public void SpeakerTwoFiveFiveNamesNOBODY_WhichIsWhyMostChoiceMenusHaveNoHeading() {
        // 255 is the literal shipped value, not our extractor rendering an absent field: the DDX
        // reader takes a signed word, so 0xFFFF would have come through as -1. It appears 221 times
        // and on 19 of the 21 ChoiceMenu entries. id+292 = 547 in a 346-entry table, so the original
        // reads past its own relocated offset array and has no valid answer either.
        Assert.False(DialogSpeakerNamePill.ResolvesToAName(255));
        Assert.False(DialogSpeakerNamePill.ResolvesToAName(244));
        Assert.False(DialogSpeakerNamePill.ResolvesToAName(0));

        // The two that DO resolve on shipped ChoiceMenu entries are party members.
        Assert.True(DialogSpeakerNamePill.ResolvesToAName(2));
        Assert.True(DialogSpeakerNamePill.ResolvesToAName(3));
        Assert.True(DialogSpeakerNamePill.IsPartySpeaker(2));

        // And the keyword half's own ends.
        Assert.True(DialogSpeakerNamePill.ResolvesToAName(7));
        Assert.True(DialogSpeakerNamePill.ResolvesToAName(53));
        Assert.False(DialogSpeakerNamePill.ResolvesToAName(54));
    }

}
