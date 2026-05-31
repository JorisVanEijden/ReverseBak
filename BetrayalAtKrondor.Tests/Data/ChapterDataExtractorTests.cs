namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Data;

using ResourceExtraction.Extractors;

using System.IO;

using Xunit;

/// <summary>
/// Verifies CHAPx.DAT parsing. The reference bytes are the real CHAP1.DAT shipped with the
/// game; they decode to chapter 1's start in zone 1 at tile (10,16) — the state the original
/// engine applies via go_to_chapter (0x41f0a) over the zeroed STARTUP.GAM template.
/// </summary>
public class ChapterDataExtractorTests {
    // CHAP1.DAT (23 bytes), verbatim.
    private static readonly byte[] Chap1 = {
        0x01, 0x00,             // chapterNumber = 1
        0x00, 0x00, 0x00, 0x00, // gold = 0
        0x00, 0xe1, 0x00, 0x00, // gameTimeIn2Seconds = 0xE100 = 57600
        0x00, 0x00, 0x00, 0x00, // timeSnapshot = 0
        0x00,                   // partyDeathState
        0x00,                   // chapterTransitionPending
        0x01,                   // zone = 1
        0x0a,                   // tileX = 10
        0x10,                   // tileY = 16
        0x12,                   // tileXOffset = 18
        0x19,                   // tileYOffset = 25
        0x00, 0x80              // zRotation = 0x8000
    };

    private static ChapterStartData Extract(byte[] bytes) =>
        new ChapterDataExtractor().Extract("CHAP1.DAT", new MemoryStream(bytes));

    [Fact]
    public void Extract_ReadsScalarFields() {
        ChapterStartData chapter = Extract(Chap1);

        Assert.Equal(1, chapter.ChapterNumber);
        Assert.Equal(0, chapter.Gold);
        Assert.Equal(57600, chapter.GameTimeIn2Seconds);
        Assert.Equal(0, chapter.TimeSnapshot);
    }

    [Fact]
    public void Extract_ReadsStartLocation() {
        ChapterStartData chapter = Extract(Chap1);

        Assert.Equal(1, chapter.StartLocation.ZoneNumber);
        Assert.Equal(10, chapter.StartLocation.X);
        Assert.Equal(16, chapter.StartLocation.Y);
        Assert.Equal(18, chapter.StartLocation.XOffset);
        Assert.Equal(25, chapter.StartLocation.YOffset);
        Assert.Equal(unchecked((short)0x8000), chapter.StartLocation.ZRotation);
    }

    [Fact]
    public void Extract_ComputesCentredGamePosition() {
        ChapterStartData chapter = Extract(Chap1);

        // tile*64000 + offset*1600 + 800 (sub-tile centring), per ToCenteredGamePosition.
        Assert.Equal(10 * 64000 + 18 * 1600 + 800, chapter.PositionX); // 669600
        Assert.Equal(16 * 64000 + 25 * 1600 + 800, chapter.PositionY); // 1064800
    }

    [Fact]
    public void Extract_ProducesChapter1FullMapMarker() {
        ChapterStartData chapter = Extract(Chap1);

        Assert.True(chapter.FullMapIcon.Visible);
        Assert.Equal(18, chapter.FullMapIcon.IconIndex);
        // (116,75) pixels on the 320x200 full map → percentages.
        Assert.Equal(116f / 320f * 100f, chapter.FullMapIcon.XPercent, 3);
        Assert.Equal(75f / 200f * 100f, chapter.FullMapIcon.YPercent, 3);
    }

    [Fact]
    public void Extract_Chapter8_HasNoFullMapMarker() {
        byte[] chap8 = (byte[])Chap1.Clone();
        chap8[0] = 0x08; // chapterNumber = 8

        ChapterStartData chapter = Extract(chap8);

        Assert.Equal(8, chapter.ChapterNumber);
        Assert.False(chapter.FullMapIcon.Visible);
    }
}
