namespace BetrayalAtKrondor.Tests.Audio;

using GameData.Resources.Audio;
using Xunit;

/// <summary>
/// Which track each context asks for. The silent overworld and the leave-it-alone book page are the
/// two a remake gets wrong by adding music the original does not play.
/// </summary>
public class MusicSelectionTests {
    private const int Outdoor = 0;
    private const int Underground = 2;

    [Fact]
    public void TheOverworldIsSilent() {
        // Not an oversight and not a missing track — an ordinary outdoor zone before chapter 8 asks
        // for silence, and what you hear out there is ambience.
        Assert.Equal(MusicPlayback.NoTrack, MusicSelection.ForZone(Outdoor, 1, 1));
        Assert.Equal(MusicPlayback.NoTrack, MusicSelection.ForZone(Outdoor, 9, 7));
    }

    [Fact]
    public void UndergroundZonesHaveTheirOwnTrack() {
        Assert.Equal(MusicSelection.UndergroundTrack, MusicSelection.ForZone(Underground, 11, 3));
    }

    [Fact]
    public void ZoneSixHasATrackOfItsOwn() {
        Assert.Equal(MusicSelection.ZoneSixTrack, MusicSelection.ForZone(Outdoor, 6, 3));
    }

    [Fact]
    public void TheFinalChapterScoresEveryOtherZone() {
        Assert.Equal(MusicSelection.FinalChapterTrack, MusicSelection.ForZone(Outdoor, 1, 8));
    }

    [Fact]
    public void UndergroundBeatsBothZoneSixAndTheFinalChapter() {
        // The tests run in order and the first wins, so chapter 8 does not re-score the caves.
        Assert.Equal(MusicSelection.UndergroundTrack, MusicSelection.ForZone(Underground, 6, 8));
    }

    [Fact]
    public void ZoneSixKeepsItsTrackIntoTheFinalChapter() {
        Assert.Equal(MusicSelection.ZoneSixTrack, MusicSelection.ForZone(Outdoor, 6, 8));
    }

    [Fact]
    public void CombatPicksOneOfThreeTracks() {
        Assert.Equal(0x40a, MusicSelection.ForCombat(0, true));
        Assert.Equal(0x3ed, MusicSelection.ForCombat(1, true));
        Assert.Equal(0x413, MusicSelection.ForCombat(2, true));
    }

    [Fact]
    public void CombatMusicOffSilencesTheEncounterRatherThanLeavingTheZoneTrack() {
        Assert.Equal(MusicPlayback.NoTrack, MusicSelection.ForCombat(0, false));
    }

    [Fact]
    public void ARollOutsideTheRangeFallsToTheLastTrackAsTheOriginalsDefaultDoes() {
        Assert.Equal(0x413, MusicSelection.ForCombat(5, true));
        Assert.Equal(0x413, MusicSelection.ForCombat(-1, true));
    }

    [Theory]
    [InlineData(0x60, 0x3ef)]
    [InlineData(0x51, 0x3ef)]
    [InlineData(0x50, 0x40f)]
    [InlineData(0x42, 0x40f)]
    [InlineData(0x41, 0x410)]
    [InlineData(0x2e, 0x410)]
    [InlineData(0x2d, 0x3f0)]
    [InlineData(0, 0x3f0)]
    public void AHealingItemsTrackGetsGrimmerAsHealthDrops(int health, int expected) {
        Assert.Equal(expected, MusicSelection.ForHealingItem(health));
    }

    [Fact]
    public void OnlyTwoBookPagesChangeTheMusic() {
        Assert.Equal(MusicSelection.BookPageTrackA, MusicSelection.ForBookPage(MusicSelection.BookPageA));
        Assert.Equal(MusicSelection.BookPageTrackB, MusicSelection.ForBookPage(MusicSelection.BookPageB));
    }

    [Fact]
    public void EveryOtherPageLeavesTheMusicAloneRatherThanStoppingIt() {
        // Returning silence here would cut the music on every page turn.
        Assert.Equal(MusicPlayback.QueryOnly, MusicSelection.ForBookPage(0x1d4));
        Assert.Equal(MusicPlayback.QueryOnly, MusicSelection.ForBookPage(0));
    }

    // ---- the options menu ----------------------------------------------------------------------

    [Fact]
    public void TheMenuHasOneTrackForBothOfItsForms() =>
        // openedFromGame picks REQ_OPT1 over REQ_OPT0; it does not change the music.
        Assert.Equal(0x3f7, MusicSelection.MainMenuTrack);

    [Fact]
    public void ClosingBackToTheGameRestoresWhatWasPlaying() =>
        Assert.True(MusicSelection.RestoresPreviousTrack(MusicSelection.MenuExit.Resume));

    [Theory]
    [InlineData(MusicSelection.MenuExit.NewGame)]
    [InlineData(MusicSelection.MenuExit.LoadGame)]
    [InlineData(MusicSelection.MenuExit.Contents)]
    [InlineData(MusicSelection.MenuExit.Quit)]
    public void LeavingForSomewhereElseDoesNotRestore(MusicSelection.MenuExit exit) =>
        // Each destination sets its own music as it comes up; putting the old track back for the
        // moment it takes to load is a stutter the original does not have.
        Assert.False(MusicSelection.RestoresPreviousTrack(exit));
}
