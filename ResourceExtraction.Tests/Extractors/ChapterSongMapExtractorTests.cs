namespace ResourceExtraction.Tests.Extractors;

using GameData.Resources.Audio;
using ResourceExtraction.Extractors;
using Xunit;

public class ChapterSongMapExtractorTests {
    [Fact]
    public void Extract_ParsesShippingLayout() {
        // Reproduces the shipping CHAPSONG.DAT byte layout (36 bytes,
        // 9 chapters × 2 × i16 LE). Sourced from the actual game file.
        byte[] bytes = [
            0xf2, 0x03, 0xf5, 0x03,  // Ch 1: 1010, 1013
            0x12, 0x04, 0x19, 0xfc,  // Ch 2: 1042, -999
            0xfb, 0x03, 0x24, 0x04,  // Ch 3: 1019, 1060
            0x05, 0x04, 0x19, 0xfc,  // Ch 4: 1029, -999
            0x07, 0x04, 0x13, 0x04,  // Ch 5: 1031, 1043
            0xe9, 0x03, 0x19, 0xfc,  // Ch 6: 1001, -999
            0x20, 0x04, 0x19, 0xfc,  // Ch 7: 1056, -999
            0x12, 0x04, 0x01, 0x04,  // Ch 8: 1042, 1025
            0xfb, 0x03, 0xf2, 0x03,  // Ch 9: 1019, 1010
        ];
        using var stream = new MemoryStream(bytes);

        ChapterSongMap map = new ChapterSongMapExtractor().Extract("CHAPSONG.DAT", stream);

        Assert.Equal(9, map.Entries.Count);
        Assert.Equal(1010, map.Entries[0].Book1Song);
        Assert.Equal(1013, map.Entries[0].Book2Song);
        Assert.Equal(1042, map.Entries[1].Book1Song);
        Assert.Equal(ChapterSongMap.NoChange, map.Entries[1].Book2Song);
        Assert.Equal(ChapterSongMap.NoChange, map.Entries[6].Book2Song);
        Assert.Equal(1010, map.Entries[8].Book2Song);
    }

    [Fact]
    public void Extract_PreservesNoChangeSentinel() {
        // -999 (0xFC19 LE) is the "leave current song" sentinel checked by
        // audio_song_sub_1505A at 0x15072. Ensure it round-trips verbatim
        // rather than being normalized.
        byte[] bytes = new byte[36];
        for (int i = 0; i < 18; i++) {
            bytes[i * 2 + 0] = 0x19;
            bytes[i * 2 + 1] = 0xfc;
        }
        using var stream = new MemoryStream(bytes);

        ChapterSongMap map = new ChapterSongMapExtractor().Extract("CHAPSONG.DAT", stream);

        foreach (ChapterSongEntry entry in map.Entries) {
            Assert.Equal(ChapterSongMap.NoChange, entry.Book1Song);
            Assert.Equal(ChapterSongMap.NoChange, entry.Book2Song);
        }
    }
}
