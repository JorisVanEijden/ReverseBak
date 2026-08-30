namespace BetrayalAtKrondor.Tests.Audio;

using GameData.Resources.Audio;
using Xunit;

/// <summary>
/// The track a chapter's intro book opens with — <c>gmain_play_chapter_intro</c> (GMAIN.C:292).
/// </summary>
/// <remarks>
/// <b>Not the same table or the same key as <c>ForBookPage</c>.</b> That one is the two pages in the
/// whole game that change the music as you turn to them; this is what plays when a chapter intro
/// book OPENS, read from CHAPSONG.DAT by (chapter, part).
/// </remarks>
public class ChapterBookMusicTests {
    private static ChapterSongMap Shipped() {
        var map = new ChapterSongMap("CHAPSONG.DAT");
        // The shipped table, as extracted.
        (short A, short B)[] rows = {
            (1010, 1013), (1042, -999), (1019, 1060), (1029, -999), (1031, 1043),
            (1001, -999), (1056, -999), (1042, 1025), (1019, 1010),
        };
        foreach ((short a, short b) in rows) {
            map.Entries.Add(new ChapterSongEntry { Book1Song = a, Book2Song = b });
        }
        return map;
    }

    [Fact]
    public void EachChapterHasATrackForBothOfItsBooks() {
        ChapterSongMap songs = Shipped();

        Assert.Equal(1010, MusicSelection.ForChapterBook(songs, 1, 1));
        Assert.Equal(1013, MusicSelection.ForChapterBook(songs, 1, 2));
        Assert.Equal(1019, MusicSelection.ForChapterBook(songs, 9, 1));
        Assert.Equal(1010, MusicSelection.ForChapterBook(songs, 9, 2));
    }

    [Fact]
    public void FOUR_ChaptersLeaveTheMusicAloneForTheirSecondBook() {
        // *** THE SENTINEL IS SHARED, AND THAT IS WHY NO SPECIAL CASE IS NEEDED. ***
        // ChapterSongMap.NoChange and MusicPlayback.QueryOnly are both -999 — the original's own
        // "leave it alone" — so the shipped value goes through the ordinary playback path untouched.
        Assert.Equal(MusicPlayback.QueryOnly, ChapterSongMap.NoChange);

        ChapterSongMap songs = Shipped();
        foreach (int chapter in new[] { 2, 4, 6, 7 }) {
            Assert.Equal(MusicPlayback.QueryOnly, MusicSelection.ForChapterBook(songs, chapter, 2));
        }

        // And those same chapters DO change it for their first book, so this is a per-book rule
        // rather than a quiet chapter.
        Assert.Equal(1042, MusicSelection.ForChapterBook(songs, 2, 1));
    }

    [Fact]
    public void AnythingOutsidePart1Or2LeavesTheMusicAlone() {
        // The original initialises trackId to -999 and only overwrites it when part is 1 or 2, so an
        // out-of-range key is "leave it alone" rather than silence. Returning NoTrack instead would
        // STOP the music on a chapter with a third part.
        ChapterSongMap songs = Shipped();

        Assert.Equal(MusicPlayback.QueryOnly, MusicSelection.ForChapterBook(songs, 1, 3));
        Assert.Equal(MusicPlayback.QueryOnly, MusicSelection.ForChapterBook(songs, 1, 0));
        Assert.Equal(MusicPlayback.QueryOnly, MusicSelection.ForChapterBook(songs, 10, 1));
        Assert.Equal(MusicPlayback.QueryOnly, MusicSelection.ForChapterBook(null, 1, 1));
    }
}
